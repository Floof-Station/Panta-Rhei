using Content.Shared.Verbs;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Administration.Logs;
using Content.Shared.Vampiric;
using Content.Server._Floof.Vampire;
// using Content.Shared.Cocoon; Not porting cocooning, thats another can of worms that I don't wanna open right now.
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Server.DoAfter;
using Content.Server.Nutrition.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.HealthExaminable;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Floof.Vampire
{
    public sealed partial class BloodSuckerSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BloodSuckerComponent, GetVerbsEvent<InnateVerb>>(AddSuccVerb);
            SubscribeLocalEvent<BloodSuckedComponent, GetVerbsEvent<InnateVerb>>(AddMarksVerb);
            SubscribeLocalEvent<BloodSuckedComponent, HealthBeingExaminedEvent>(OnHealthExamined);

            // SubscribeLocalEvent<BloodSuckerComponent, ComponentStartup>(OnStartup); // This aint used, why did den keep it?
            // SubscribeLocalEvent<BloodSuckerComponent, ComponentShutdown>(OnShutdown); // This aint used either
            SubscribeLocalEvent<BloodSuckerComponent, BloodSuckDoAfterEvent>(OnDoAfter);
        }

        private void AddSuccVerb(EntityUid uid, BloodSuckerComponent component, GetVerbsEvent<InnateVerb> args)
        {
            var victim = args.Target;
            var ignoreClothes = false;

            // if (TryComp<CocoonComponent>(args.Target, out var cocoon))
            // {
            //     victim = cocoon.Victim ?? args.Target;
            //     ignoreClothes = cocoon.Victim != null;
            // }
            // else if (component.WebRequired)
            //     return;

            if (!TryComp<BloodstreamComponent>(victim, out var bloodstream) || args.User == victim || !args.CanAccess)
                return;

            InnateVerb drinkBlood = new()
            {
                Act = () =>
                {
                    StartSuccDoAfter(uid, victim, component, bloodstream, !ignoreClothes); // start doafter
                },
                Text = Loc.GetString("action-name-suck-blood"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Nyanotrasen/Icons/verbiconfangs.png")),
                Priority = 2
            };

            InnateVerb wipeMarks = new()
            {
                Act = () =>
                {
                    DoHideMarksVerb(uid, victim);
                },
                Text = Loc.GetString("bloodsucker-wipe-marks-verb"),
                Priority = 1
            };

            args.Verbs.Add(drinkBlood);

            if (HasComp<BloodSuckedComponent>(victim))
            {
                args.Verbs.Add(wipeMarks);
            }
        }

        private void AddMarksVerb(EntityUid uid, BloodSuckedComponent component, GetVerbsEvent<InnateVerb> args)
        {
            var victim = args.Target;

            // if (TryComp<CocoonComponent>(args.Target, out var cocoon))
            // {
            //     victim = cocoon.Victim ?? args.Target;
            // }

            InnateVerb wipeMarks = new()
            {
                Act = () =>
                {
                    DoHideMarksVerb(uid, victim);
                },
                Text = Loc.GetString("bloodsucker-wipe-marks-verb"),
                Priority = 1
            };

            if (HasComp<BloodSuckedComponent>(victim))
            {
                args.Verbs.Add(wipeMarks);
            }
        }

        private void DoHideMarksVerb(EntityUid uid, EntityUid victim)
        {
            if (!HasComp<BloodSuckedComponent>(victim))
                return;

            RemComp<BloodSuckedComponent>(victim);
            _popups.PopupEntity(Loc.GetString("bloodsucker-wipe-marks-popup", ("sucker", uid), ("target", victim)), victim);
        }

        private void OnHealthExamined(EntityUid uid, BloodSuckedComponent component, HealthBeingExaminedEvent args)
        {
            // Floof: allow empty messages for basic examine
            if (!args.Message.IsEmpty)
                args.Message.PushNewline();
            args.Message.AddMarkupPermissive(Loc.GetString("bloodsucked-health-examine", ("target", uid)));
        }

        private void OnDoAfter(EntityUid uid, BloodSuckerComponent component, BloodSuckDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            args.Handled = TrySucc(uid, args.Args.Target.Value);
        }

        public void StartSuccDoAfter(EntityUid bloodsucker, EntityUid victim, BloodSuckerComponent? bloodSuckerComponent = null, BloodstreamComponent? stream = null, bool doChecks = true)
        {
            if (!Resolve(bloodsucker, ref bloodSuckerComponent) || !Resolve(victim, ref stream))
                return;

            if (doChecks)
            {
                if (!_interactionSystem.InRangeUnobstructed(bloodsucker, victim))
                    return;

                if (_inventorySystem.TryGetSlotEntity(victim, "head", out var headUid) && HasComp<PressureProtectionComponent>(headUid))
                {
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-helmet", ("helmet", headUid)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
                    return;
                }

                if (_inventorySystem.TryGetSlotEntity(bloodsucker, "mask", out var maskUid) &&
                    EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
                    blocker.Enabled)
                {
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-mask", ("mask", maskUid)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
                    return;
                }
            }
            //Fuck my stupid fox life this code makes me want to rip my hair out. It doesnt work, we dont need it, and if a vampire tries to drink from a xeno they deserve it.
            //if (_prototypeManager.TryIndex(stream.BloodReagents, out var bloodReagent) && !bloodReagent.IsNourishing)
            //    _popups.PopupEntity(Loc.GetString("bloodsucker-not-blood", ("target", victim)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
            if (_solutionSystem.PercentFull(victim) != 0)
                _popups.PopupEntity(Loc.GetString("bloodsucker-fail-no-blood", ("target", victim)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
            else
                _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start", ("target", victim)), victim, bloodsucker, Shared.Popups.PopupType.Medium);

            _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start-victim", ("sucker", bloodsucker)), victim, victim, Shared.Popups.PopupType.LargeCaution);

            var args = new DoAfterArgs(EntityManager, bloodsucker, bloodSuckerComponent.Delay, new BloodSuckDoAfterEvent(), bloodsucker, target: victim)
            {
                BreakOnMove = false,
                DistanceThreshold = 2f,
                NeedHand = false
            };

            _doAfter.TryStartDoAfter(args);
        }

        public bool TrySucc(EntityUid bloodsucker, EntityUid victim, BloodSuckerComponent? bloodsuckerComp = null)
        {
            // Is bloodsucker a bloodsucker?
            if (!Resolve(bloodsucker, ref bloodsuckerComp))
                return false;

            // Does victim have a bloodstream?
            if (!TryComp<BloodstreamComponent>(victim, out var bloodstream))
                return false;

            // No blood left, yikes.
            if (_bloodstreamSystem.GetBloodLevelPercentage(victim) == 0.0f)
                return false;

            // Does bloodsucker have a stomach?
            var stomachList = _bodySystem.GetBodyOrganEntityComps<StomachComponent>(bloodsucker);
            if (stomachList.Count == 0)
                return false;

            if (!_solutionSystem.TryGetSolution(stomachList[0].Owner, StomachSystem.DefaultSolutionName, out _))
                return false;

            // Are we too full?
            if (_solutionSystem.PercentFull(bloodsucker) >= 1)
            {
                _popups.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), bloodsucker, bloodsucker, Shared.Popups.PopupType.MediumCaution);
                return false;
            }

            _adminLogger.Add(Shared.Database.LogType.MeleeHit, Shared.Database.LogImpact.Medium, $"{ToPrettyString(bloodsucker):player} sucked blood from {ToPrettyString(victim):target}");

            // All good, succ time.
            _audio.PlayPvs(bloodsuckerComp.DrinkSound, bloodsucker);
            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked-victim", ("sucker", bloodsucker)), victim, victim, Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked", ("target", victim)), bloodsucker, bloodsucker, Shared.Popups.PopupType.Medium);
            EnsureComp<BloodSuckedComponent>(victim);

            // Make everything actually ingest.
            if (bloodstream.BloodSolution == null)
                return false;

            var temp = _solutionSystem.SplitSolution(bloodstream.BloodSolution.Value, bloodsuckerComp.UnitsToSucc);
            _stomachSystem.TryTransferSolution(stomachList[0].Owner, temp, stomachList[0].Comp1);

            // Add a little pierce
            DamageSpecifier damage = new();
            damage.DamageDict.Add("Piercing", 1); // Slowly accumulate enough to gib after like half an hour

            _damageableSystem.TryChangeDamage(victim, damage, true);

            //I'm not porting the nocturine gland, this code is deprecated, and will be reworked at a later date.
            //if (bloodsuckerComp.InjectWhenSucc && _solutionSystem.TryGetInjectableSolution(victim, out var injectable))
            //{
            //    _solutionSystem.TryAddReagent(victim, injectable, bloodsuckerComp.InjectReagent, bloodsuckerComp.UnitsToInject, out var acceptedQuantity);
            //}
            return true;
        }
    }
}
