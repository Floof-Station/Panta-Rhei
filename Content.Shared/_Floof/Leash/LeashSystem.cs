using System.Linq;
using Content.Shared._Floof.Leash.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Floof.Leash;

// TODO this system is a nightmare
public sealed partial class LeashSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfters = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        InitializeVerbs();
        InitializeRopes();

        SubscribeLocalEvent<LeashAnchorComponent, BeingUnequippedAttemptEvent>(OnAnchorUnequipping);

        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(OnRequestPullLeash), before: [typeof(PullingSystem)])
            .Register<LeashSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<LeashSystem>();
    }

    private void OnAnchorUnequipping(Entity<LeashAnchorComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        // Prevent unequipping the anchor clothing until the leash is removed
        if (TryGetLeashTarget(args.Equipment, out var leashTarget)
            && TryComp<LeashedComponent>(leashTarget, out var leashed)
            && leashed.Leash is not null
            && GetEntity(leashed.Anchor) == args.Equipment
           )
            args.Cancel();
    }

    private bool OnRequestPullLeash(ICommonSession? session, EntityCoordinates targetCoords, EntityUid uid)
    {
        if (_net.IsClient
            || session?.AttachedEntity is not { } player
            || !player.IsValid()
            || !_hands.TryGetActiveItem(player, out var leash)
            || !TryComp<LeashComponent>(leash, out var leashComp)
            || !leashComp.PullInterval.TryUpdate(_timing))
            return false;

        // find the entity closest to the target coords
        var candidates = leashComp.Leashed
            .Select(it => GetEntity(it.Pulled))
            .Where(it => it != EntityUid.Invalid)
            .Select(it => (it, Transform(it).Coordinates.TryDistance(EntityManager, _xform, targetCoords, out var dist) ? dist : float.PositiveInfinity))
            .Where(it => it.Item2 < float.PositiveInfinity)
            .ToList();

        if (candidates.Count == 0)
            return false;

        // And pull it towards the user
        var pulled = candidates.MinBy(it => it.Item2).Item1;
        var playerCoords = Transform(player).Coordinates;
        var pulledCoords = Transform(pulled).Coordinates;
        var pullDir = _xform.ToMapCoordinates(playerCoords).Position - _xform.ToMapCoordinates(pulledCoords).Position;

        _throwing.TryThrow(pulled, pullDir * 0.4f, user: player, pushbackRatio: 1f, animated: false, recoil: false, playSound: false, doSpin: false);
        return true;
    }

    /// <summary>
    ///     Tries to find the entity this anchor is attached to and returns it. May return EntityUid.Invalid.
    /// </summary>
    private Entity<LeashedComponent?> GetLeashed(Entity<LeashAnchorComponent> anchor)
    {
        if (!TryGetLeashTarget(anchor!, out var leashTarget))
            return EntityUid.Invalid;

        return (leashTarget, CompOrNull<LeashedComponent>(leashTarget));
    }

    /// <summary>
    ///     Checks if the specified mob should be able to interact with the leash (e.g. configure its length).
    /// </summary>
    private bool CanInteractWithLeash(EntityUid user, Entity<LeashComponent> leash)
    {
        // Don't allow the leashed person to interact with it unless they are actively holding it.
        // This is to prevent e.g. a leashed-and-anchored mob from changing their leash length. Other people however may tinker with it.
        if (!TryComp<LeashedComponent>(user, out var leashed) || leashed.Leash != GetNetEntity(leash))
            return true;

        return _xform.ContainsEntity(user, leash.Owner);
    }

    /// <summary>
    ///     Tries to find the entity that gets leashed for the given anchor entity.
    /// </summary>
    public bool TryGetLeashTarget(Entity<LeashAnchorComponent?> anchor, out EntityUid leashTarget)
    {
        leashTarget = default;
        if (!Resolve(anchor, ref anchor.Comp, false))
            return false;

        if (anchor.Comp.Kind.HasFlag(LeashAnchorComponent.AnchorKind.Clothing)
            && TryComp<ClothingComponent>(anchor, out var clothing)
            && clothing.InSlot != null
            && _container.TryGetContainingContainer(anchor.Owner, out var container))
        {
            leashTarget = container.Owner;
            return true;
        }

        if (anchor.Comp.Kind.HasFlag(LeashAnchorComponent.AnchorKind.Intrinsic))
        {
            leashTarget = anchor.Owner;
            return true;
        }

        return false;
    }

    public bool CanLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash)
    {
        return leash.Comp.Leashed.Count < leash.Comp.MaxJoints
            && GetLeashed(anchor).Comp?.Leash == null
            && Transform(anchor).Coordinates.TryDistance(EntityManager, Transform(leash).Coordinates, out var dst)
            && _entCfg.TryGetConfigFloat(leash.Owner, "length", out var length) && dst <= length;
    }

    /// <summary>
    ///     Start a do-after to try to leash the specified entity.
    /// </summary>
    public bool TryStartLeashing(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash, EntityUid user, bool popup = true)
    {
        if (!CanLeash(anchor, leash) || !TryGetLeashTarget(anchor!, out var leashTarget))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, leash.Comp.AttachDelay, new LeashAttachDoAfterEvent(), anchor, leashTarget, leash)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = true
        };

        var result = _doAfters.TryStartDoAfter(doAfter);
        if (result && _net.IsServer && popup)
        {
            (string, object)[] locArgs = [("user", user), ("target", leashTarget), ("anchor", anchor.Owner), ("selfAnchor", anchor.Owner == leashTarget)];

            _popups.PopupEntity(Loc.GetString("leash-attaching-popup-self", locArgs), user, user);
            if (user != leashTarget)
                _popups.PopupEntity(Loc.GetString("leash-attaching-popup-target", locArgs), leashTarget, leashTarget);

            var othersFilter = Filter.PvsExcept(leashTarget).RemovePlayerByAttachedEntity(user);
            _popups.PopupEntity(Loc.GetString("leash-attaching-popup-others", locArgs), leashTarget, othersFilter, true);
        }
        return result;
    }

    /// <summary>
    ///     Start a do-after to remove the leash from the specified entity.
    /// </summary>
    public bool TryStartUnleashing(Entity<LeashedComponent?> leashed, Entity<LeashComponent?> leash, EntityUid user, bool popup = true)
    {
        if (!Resolve(leashed, ref leashed.Comp, false)
            || !Resolve(leash, ref leash.Comp)
            || leashed.Comp.Leash != GetNetEntity(leash))
            return false;

        // Apply a longer delay if the user tries to unleash themselves while NOT holding the leash
        var delay = (user == leashed.Owner && !_xform.IsParentOf(Transform(leashed), leash))
            ? leash.Comp.SelfDetachDelay
            : leash.Comp.DetachDelay;

        var doAfter = new DoAfterArgs(EntityManager, user, delay, new LeashDetachDoAfterEvent(), leashed.Owner, leashed)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = true
        };

        var result = _doAfters.TryStartDoAfter(doAfter);
        if (result && _net.IsServer)
        {
            (string, object)[] locArgs = [("user", user), ("target", leashed.Owner), ("isSelf", user == leashed.Owner)];
            _popups.PopupEntity(Loc.GetString("leash-detaching-popup-self", locArgs), user, user);
            _popups.PopupEntity(Loc.GetString("leash-detaching-popup-others", locArgs), user, Filter.PvsExcept(user), true);
        }

        return result;
    }

    /// <summary>
    ///     Immediately creates the leash joint between the specified entities and sets up respective components.
    /// </summary>
    /// <param name="anchor">The anchor entity, usually either target's clothing or the target itself.</param>
    /// <param name="leash">The leash entity.</param>
    /// <param name="leashTarget">The entity to which the leash is actually connected. Can be EntityUid.Invalid, then it will be deduced.</param>
    /// <param name="force">Whether to bypass range checks.</param>
    public bool TryLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash, EntityUid leashTarget, bool force = false)
    {
        if (_net.IsClient)
            return true;

        if (leashTarget is { Valid: false } && !TryGetLeashTarget(anchor!, out leashTarget))
            return false;

        // Do not allow to leash the same person twice, this horribly breaks everything
        if (TryComp<LeashedComponent>(leashTarget, out var leashedComp)
            && leashedComp.Leash is not null)
            return false;

        leashedComp = EnsureComp<LeashedComponent>(leashTarget);
        var netLeashTarget = GetNetEntity(leashTarget);
        var data = new LeashComponent.LeashData(null, netLeashTarget);

        leashedComp.Leash = GetNetEntity(leash);
        leashedComp.Anchor = GetNetEntity(anchor);

        leash.Comp.Leashed.Add(data);
        Dirty(leash);

        // This should actually create the new rope
        RefreshRopes(leash, false);
        return true;
    }

    public void RemoveLeash(Entity<LeashedComponent?> leashed, Entity<LeashComponent?> leash)
    {
        if (_net.IsClient || !Resolve(leashed, ref leashed.Comp))
            return;

        leashed.Comp.Anchor = leashed.Comp.Leash = null; // Just so other methods can't re-create it.
        RemCompDeferred<LeashedComponent>(leashed); // Has to be deferred else the client explodes for some reason

        if (Resolve(leash, ref leash.Comp, false))
        {
            var netLeashed = GetNetEntity(leashed);
            var leashedData = leash.Comp.Leashed.Where(it => it.Pulled == netLeashed).ToList();

            foreach (var data in leashedData)
            {
                leash.Comp.Leashed.Remove(data); // Doing this first to avoid recursion
                if (TryGetEntity(data.Rope, out var rope))
                    QueueDel(rope);
            }
        }

        Dirty(leash);
    }
}
