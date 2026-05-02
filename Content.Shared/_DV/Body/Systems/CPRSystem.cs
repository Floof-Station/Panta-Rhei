using Content.Shared._DV.Body.Components;
using Content.Shared._DV.Body.Events;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
﻿using Content.Shared.Atmos.Rotting;;
using Content.Server.Atmos.Rotting; // Euphoria
using Content.Server.DoAfter;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Traits.Assorted.Components;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._DV.Body.Systems;

public sealed class CPRSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    // Start Euphoria //
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly FoodSystem _foodSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly RottingSystem _rottingSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    // End Euphoria // 

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanDoCPRComponent, CPRFinishedEvent>(OnCprFinished);
        SubscribeLocalEvent<MobStateComponent, GetVerbsEvent<AlternativeVerb>>(AddCPRVerb);
    }

    private void OnCprFinished(Entity<CanDoCPRComponent> entity, ref CPRFinishedEvent ev)
    {
        if (!ev.Target.HasValue || ev.Handled)
            return;

        // Euphoria Start
        if (ev.Cancelled || ev.Handled || !ev.Target.HasValue)
        {
            entity.Comp.CPRPlayingStream = _audio.Stop(entity.Comp.CPRPlayingStream);
            return;
        }
        if (!entity.Comp.CPRHealing.Empty)
            _damageable.TryChangeDamage(args.Target, entity.Comp.CPRHealing, true, origin: entity);

        if (entity.Comp.RotReductionMultiplier > 0)
            _rottingSystem.ReduceAccumulator(
                (EntityUid)args.Target, entity.Comp.TimeLength * entity.Comp.RotReductionMultiplier);

        if (_robustRandom.Prob(entity.Comp.ResuscitationChance)
            && !HasComp<UnrevivableComponent>(args.Target.Value) // Floofstation - unrevivable
            && _mobThreshold.TryGetThresholdForState((EntityUid)args.Target, MobState.Dead, out var threshold)
            && TryComp<DamageableComponent>(args.Target, out var damageableComponent)
            && TryComp<MobStateComponent>(args.Target, out var state)
            && damageableComponent.TotalDamage < threshold)
            _mobStateSystem.ChangeMobState(args.Target.Value, MobState.Critical, state, entity);

        var isAlive = _mobStateSystem.IsAlive(args.Target.Value);
        args.Repeat = !isAlive;
        if (isAlive)
            entity.Comp.CPRPlayingStream = _audio.Stop(entity.Comp.CPRPlayingStream);
        // Euphoria End

        if (!ev.Cancelled && !_mobStateSystem.IsAlive(ev.Target.Value)) // Allow CPR on Criticial and Dead patients.
        {
            var comp = EnsureComp<AffectedByCPRComponent>(ev.Target.Value); // Enables the Crit Patient to breathe.
            comp.IsActive = true;
            ev.Repeat = true;

            var msgUser = Loc.GetString("cpr-popup-continue-user", ("patient", ev.Target.Value));
            var msgOthers = Loc.GetString("cpr-popup-continue-others", ("patient", ev.Target.Value), ("provider", entity.Owner));
            _popupSystem.PopupPredicted(msgUser, msgOthers, entity.Owner, entity.Owner);
        }
        else
        {
            RemComp<AffectedByCPRComponent>(ev.Target.Value); // Removes breathing while crit.

            var msgUser = Loc.GetString("cpr-popup-stop-user", ("patient", ev.Target.Value));
            var msgOthers = Loc.GetString("cpr-popup-stop-others", ("patient", ev.Target.Value), ("provider", entity.Owner));
            _popupSystem.PopupPredicted(msgUser, msgOthers, entity.Owner, entity.Owner);
        }
        ev.Handled = true;
    }

    private void StartCPR(EntityUid user, EntityUid target, float cprTime)
    {
        if (HasComp<AffectedByCPRComponent>(target))
            return;

        // Start Euphoria - These line means they need their outer clothes and mouths unblocked to receive CPR
        if (_inventory.TryGetSlotEntity(target, "outerClothing", out var outer))
        {
            _popupSystem.PopupEntity(Loc.GetString("cpr-must-remove", ("clothing", outer)), user, user);
            return;
        }

        if (_foodSystem.IsMouthBlocked(user, user) || _foodSystem.IsMouthBlocked(target, user))
            return;
        // End Euphoria 

        var doAfterArgs = new DoAfterArgs(EntityManager, user, cprTime, new CPRFinishedEvent(), user, target: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnHandChange = false,
            NeedHand = true,
        };

        // Start Euphoria - CPR noises
        _doAfterSystem.TryStartDoAfter(doAfterArgs);

        user.Comp.CPRPlayingStream = _audio.Stop(user.Comp.CPRPlayingStream); // Floofstation - fix any previous CPR sounds
        var playingStream = _audio.PlayPvs(user.Comp.CPRSound, user, AudioParams.Default.WithLoop(true));
        if (!playingStream.HasValue)
            return;

        user.Comp.CPRPlayingStream = playingStream.Value.Entity;
        // End Euphoria

        AddComp<AffectedByCPRComponent>(target);
        var msgUser = Loc.GetString("cpr-popup-start-user", ("patient", target));
        var msgOthers = Loc.GetString("cpr-popup-start-others", ("patient", target), ("provider", user));
        _popupSystem.PopupPredicted(msgUser, msgOthers, user, user, PopupType.Medium);
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void AddCPRVerb(Entity<MobStateComponent> entity, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (entity.Owner == ev.User
            || !ev.CanInteract
            || _mobStateSystem.IsAlive(entity.Owner)
            || !TryComp<CanDoCPRComponent>(ev.User, out var cprComp))
            return;

        var alreadyAffected = HasComp<AffectedByCPRComponent>(ev.Target);

        var user = ev.User;
        var target = ev.Target;
        AlternativeVerb verb = new()
        {
            Act = () => StartCPR(user, target, cprComp.TimeLength),
            Text = Loc.GetString("cpr-verb-start"),
            Priority = 2,
            Disabled = alreadyAffected,
            Message = alreadyAffected ? Loc.GetString("cpr-verb-disabled-description") : Loc.GetString("cpr-verb-description"),
        };

        ev.Verbs.Add(verb);
    }
}
