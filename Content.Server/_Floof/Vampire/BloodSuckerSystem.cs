using System.Linq;
using Content.Server._Floof.InteractionVerbs.Actions.Lewd;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Floof.Clothing.SlotBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.HealthExaminable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Vampiric;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Floof.Vampire;

// Note: this system has been heavily rewritten on Euph/Panta-Rhei
public sealed class BloodSuckerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly SlotBlockerSystem _slotBlockers = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly StomachSystem _stomachSystem = default!;
    [Dependency] private readonly PuddleSystem _puddles = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodSuckerComponent, GetVerbsEvent<InnateVerb>>(AddSuccVerb);
        SubscribeLocalEvent<BloodSuckedComponent, HealthBeingExaminedEvent>(OnHealthExamined);
        SubscribeLocalEvent<BloodSuckedComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodSuckerComponent, BloodSuckDoAfterEvent>(OnDoAfter);
    }

    private void AddSuccVerb(EntityUid uid, BloodSuckerComponent component, GetVerbsEvent<InnateVerb> args)
    {
        var victim = args.Target;
        var ignoreClothes = false;

        if (!TryComp<BloodstreamComponent>(victim, out var bloodstream) || args.User == victim || !args.CanAccess)
            return;

        InnateVerb verb = new()
        {
            Act = () =>
            {
                StartSuccDoAfter(uid, victim, component, bloodstream, !ignoreClothes); // start doafter
            },
            Text = Loc.GetString("action-name-suck-blood"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Nyanotrasen/Icons/verbiconfangs.png")),
            Priority = 2,
        };
        args.Verbs.Add(verb);
    }

    private void OnHealthExamined(EntityUid uid, BloodSuckedComponent component, HealthBeingExaminedEvent args)
    {
        // Floof: allow empty messages for basic examine
        if (!args.Message.IsEmpty)
            args.Message.PushNewline();
        args.Message.AddMarkupPermissive(Loc.GetString("bloodsucked-health-examine", ("target", uid)));
    }

    private void OnDamageChanged(EntityUid uid, BloodSuckedComponent component, DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            return;

        // Check if any damage type dealt when biting is still present. If so, don't remove the flavor text yet.
        var positives = _damageableSystem.GetPositiveDamage((uid, args.Damageable));
        foreach (var damageType in component.RemoveWhenNoDamage)
        {
            if (positives.DamageDict.TryGetValue(damageType, out var damage) && damage > 0)
                return;
        }

        RemComp<BloodSuckedComponent>(uid);
    }

    private void OnDoAfter(EntityUid uid, BloodSuckerComponent component, BloodSuckDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        args.Handled = TrySucc(uid, args.Args.Target.Value);
    }

    public void StartSuccDoAfter(EntityUid bloodsucker,
        EntityUid victim,
        BloodSuckerComponent? bloodSuckerComponent = null,
        BloodstreamComponent? stream = null,
        bool doChecks = true)
    {
        if (!Resolve(bloodsucker, ref bloodSuckerComponent) || !Resolve(victim, ref stream))
            return;

        if (doChecks)
        {
            if (!_interactionSystem.InRangeUnobstructed(bloodsucker, victim))
                return;

            if (_slotBlockers.IsSlotObstructedOrOccupied(
                    bloodsucker,
                    (EntityUid?)null,
                    SlotBlockerSystem.CheckType.IgnoreBlockerPreference,
                    bloodSuckerComponent.RequiredFreeSlot,
                    out var failReason))
            {
                _popups.PopupEntity(Loc.GetString(failReason ?? "unknown"), victim, bloodsucker, PopupType.Medium);
                return;
            }
        }

        if (stream.BloodSolution?.Comp?.Solution?.Volume < bloodSuckerComponent.UnitsToSucc)
        {
            _popups.PopupEntity(Loc.GetString("bloodsucker-fail-no-blood", ("target", victim)), victim, bloodsucker, PopupType.Medium);
            return;
        }

        _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start", ("target", victim)), victim, bloodsucker, PopupType.Medium);
        _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start-victim", ("sucker", bloodsucker)), victim, victim, PopupType.LargeCaution);

        var args = new DoAfterArgs(EntityManager, bloodsucker, bloodSuckerComponent.Delay, new BloodSuckDoAfterEvent(), bloodsucker, victim)
        {
            BreakOnMove = false,
            DistanceThreshold = 2f,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    public bool TrySucc(EntityUid bloodsucker, EntityUid victim, BloodSuckerComponent? bloodsuckerComp = null)
    {
        if (!Resolve(bloodsucker, ref bloodsuckerComp)
            || !TryComp<BloodstreamComponent>(victim, out var bloodstream)
            || !TryComp<BodyComponent>(bloodsucker, out var suckerBody)
            || bloodstream.BloodSolution is not {} victimBloodSol
            || suckerBody.Organs is not {} suckerOrgans)
            return false;

        var suckedBloodSol = _solutionSystem.SplitSolution(victimBloodSol, bloodsuckerComp.UnitsToSucc);
        if (suckedBloodSol.Volume < FixedPoint2.Epsilon)
            return false;

        _damageableSystem.TryChangeDamage(victim, bloodsuckerComp.BiteDamage, true);
        _audio.PlayPvs(bloodsuckerComp.BiteSound, bloodsucker);
        _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked-victim", ("sucker", bloodsucker)), victim, victim, PopupType.LargeCaution);
        _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked", ("target", victim)), bloodsucker, bloodsucker, PopupType.Medium);
        _adminLogger.Add(LogType.Damaged, LogImpact.Medium, $"{ToPrettyString(bloodsucker):player} sucked blood from {ToPrettyString(victim):target}");

        var sucked = EnsureComp<BloodSuckedComponent>(victim);
        sucked.RemoveWhenNoDamage = bloodsuckerComp.BiteDamage.DamageDict.Keys.ToList();

        if (LewdDrinkFromOrgan.GetBiggestStomach(bloodsucker, EntityManager, _solutionSystem) is not {} stomach
            || !_stomachSystem.CanTransferSolution(stomach, suckedBloodSol, stomach))
        {
            _popups.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), bloodsucker, bloodsucker, PopupType.MediumCaution);
            _puddles.TrySpillAt(bloodsucker, suckedBloodSol, out _);
            return false;
        }

        _stomachSystem.TryTransferSolution(stomach, suckedBloodSol, stomach);

        return true;
    }
}
