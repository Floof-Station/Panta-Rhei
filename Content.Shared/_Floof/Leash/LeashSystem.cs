using System.Linq;
using Content.Shared._Floof.Leash.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
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
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    #region Lifecycle

    public override void Initialize()
    {
        InitializeVerbs();
        InitializeContainerWorkarounds();
        InitializeJoints();
        InitializePrediction();
        StartThinkingWithPortals();

        UpdatesBefore.Add(typeof(SharedPhysicsSystem));

        SubscribeLocalEvent<LeashAnchorComponent, BeingUnequippedAttemptEvent>(OnAnchorUnequipping);
        SubscribeLocalEvent<LeashAnchorComponent, GetVerbsEvent<EquipmentVerb>>(OnGetEquipmentVerbs);

        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(OnRequestPullLeash), before: [typeof(PullingSystem)])
            .Register<LeashSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<LeashSystem>();
    }

    public override void Update(float frameTime)
    {
        // Process pending updates first
        // Those entities have recently had their leash joints broken by RobustToolbox, we need to figure out if it's something we can fix
        if (_net.IsServer)
            foreach (var (leash, leashed, anchor) in _pendingJointUpdates)
                ProcessPendingJointUpdate(leash, leashed, anchor);
        _pendingJointUpdates.Clear();

        var leashQuery = EntityQueryEnumerator<LeashComponent, PhysicsComponent>();
        while (leashQuery.MoveNext(out var leashEnt, out var leash, out var physics))
        {
            var sourceXForm = Transform(leashEnt);
            foreach (var data in leash.Leashed.ToList())
                UpdateLeash(data, sourceXForm, leash, leashEnt);

            RefreshRelays((leashEnt, leash, sourceXForm));
        }
        leashQuery.Dispose();
    }

    private void UpdateLeash(LeashComponent.LeashData data, TransformComponent sourceXForm, LeashComponent leash, EntityUid leashEnt)
    {
        if (data.Pulled == NetEntity.Invalid || !TryGetEntity(data.Pulled, out var target))
            return;

        DistanceJoint? joint = null;
        if (data.JointId is not null
            && TryComp<JointComponent>(target, out var jointComp)
            && jointComp.GetJoints.TryGetValue(data.JointId, out var _joint)
        )
            joint = _joint as DistanceJoint;

        // Client: set max distance to infinity to prevent the client from ever predicting leashes.
        if (_net.IsClient)
        {
            if (joint is not null && !ShouldPredictLeashes())
                joint.MaxLength = float.MaxValue;

            return;
        }

        // Server: break each leash joint whose entities are on different maps or are too far apart
        var targetXForm = Transform(target.Value);
        if (targetXForm.MapUid != sourceXForm.MapUid
            || !sourceXForm.Coordinates.TryDistance(EntityManager, targetXForm.Coordinates, out var dst)
            || dst > leash.MaxDistance)
        {
            RemoveLeash(target.Value, (leashEnt, leash));
            _popups.PopupEntity(Loc.GetString("leash-snap-popup", ("leash", leashEnt)), target.Value);
        }

        // Server: update leash lengths if necessary/possible
        // The length can be increased freely, but can only be decreased if the pulled entity is close enough
        // TODO this never worked and probably because of joint.Length not actually containing the length between the entities
        if (joint is not null && joint.MaxLength > leash.Length && joint.Length < joint.MaxLength)
            joint.MaxLength = Math.Max(joint.Length, leash.Length);

        if (joint is not null && joint.MaxLength < leash.Length)
            joint.MaxLength = leash.Length;
    }

    #endregion

    #region event handling

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

    private void OnGetEquipmentVerbs(Entity<LeashAnchorComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanInteract
            || !TryGetLeashTarget(ent!, out var leashTarget)
            || !_interaction.InRangeUnobstructed(args.User, leashTarget) // Can't use CanAccess here since clothing
            || args.Using is not { } leash
            || !TryComp<LeashComponent>(leash, out var leashComp))
            return;

        var user = args.User;
        var leashVerb = new EquipmentVerb { Text = Loc.GetString("verb-leash-text") };

        if (CanLeash(ent, (leash, leashComp)))
            leashVerb.Act = () => TryLeash(ent, (leash, leashComp), user);
        else
        {
            leashVerb.Message = Loc.GetString("verb-leash-error-message");
            leashVerb.Disabled = true;
        }

        args.Verbs.Add(leashVerb);


        if (!TryComp<LeashedComponent>(leashTarget, out var leashedComp)
            || leashedComp.Leash != GetNetEntity(leash)
            || HasComp<LeashedComponent>(ent)) // This one means that OnGetLeashedVerbs will add a verb to remove it
            return;

        var unleashVerb = new EquipmentVerb
        {
            Text = Loc.GetString("verb-unleash-text"),
            Act = () => TryUnleash((leashTarget, leashedComp), (leash, leashComp), user)
        };
        args.Verbs.Add(unleashVerb);
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

        _throwing.TryThrow(pulled, pullDir * 0.6f, user: player, pushbackRatio: 1f, animated: false, recoil: false, playSound: false, doSpin: false);
        return true;
    }

    #endregion

    #region private api

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

    #endregion

    #region public api

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
        // Note: we don't actually care if there's a joint - that thing can be missing if CanCreateJoint is false.
        return leash.Comp.Leashed.Count < leash.Comp.MaxJoints
            && GetLeashed(anchor).Comp?.Leash == null
            && Transform(anchor).Coordinates.TryDistance(EntityManager, Transform(leash).Coordinates, out var dst)
            && dst <= leash.Comp.Length;
    }

    /// <summary>
    ///     Start a do-after to try to leash the specified entity.
    /// </summary>
    public bool TryLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash, EntityUid user, bool popup = true)
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

            // This could've been much easier if my interaction verbs PR got merged already, but it isn't yet, so I gotta suffer
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
    public bool TryUnleash(Entity<LeashedComponent?> leashed, Entity<LeashComponent?> leash, EntityUid user, bool popup = true)
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
    public void DoLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash, EntityUid leashTarget, bool force = false)
    {
        if (_net.IsClient || leashTarget is { Valid: false } && !TryGetLeashTarget(anchor!, out leashTarget))
            return;

        // Do not allow to leash the same person twice, this horribly breaks everything
        if (TryComp<LeashedComponent>(leashTarget, out var leashedComp)
            && leashedComp.JointId is not null
            && TryComp<JointComponent>(leashTarget, out var existingJointComp)
            && existingJointComp.GetJoints.ContainsKey(leashedComp.JointId))
            return;

        // Do not allow to create the joint if the target is too far away - this is mostly to prevent re-creating leashes after teleportation
        if (!force &&
            Transform(anchor).Coordinates.TryDistance(EntityManager, Transform(leash).Coordinates, out var dst) &&
            dst > leash.Comp.MaxDistance)
            return;

        leashedComp = EnsureComp<LeashedComponent>(leashTarget);
        var netLeashTarget = GetNetEntity(leashTarget);
        var data = new LeashComponent.LeashData(null, netLeashTarget);

        leashedComp.Leash = GetNetEntity(leash);
        leashedComp.Anchor = GetNetEntity(anchor);

        if (CanCreateJoint(leashTarget, leash))
        {
            var jointId = $"{LeashJointIdPrefix}{netLeashTarget}";
            var joint = CreateLeashJoint(jointId, leash, leashTarget);
            data.JointId = leashedComp.JointId = jointId;
        }
        else
        {
            leashedComp.JointId = null;
        }

        if (leash.Comp.LeashSprite is { } sprite)
        {
            _container.EnsureContainer<ContainerSlot>(leashTarget, LeashedComponent.VisualsContainerName);
            if (EntityManager.TrySpawnInContainer(null, leashTarget, LeashedComponent.VisualsContainerName, out var visualEntity))
            {
                var visualComp = EnsureComp<LeashedVisualsComponent>(visualEntity.Value);
                visualComp.Sprite = sprite;
                visualComp.Source = leash;
                visualComp.Target = leashTarget;
                visualComp.OffsetTarget = anchor.Comp.Offset;

                data.LeashVisuals = GetNetEntity(visualEntity);
            }
        }

        leash.Comp.Leashed.Add(data);
        Dirty(leash);
    }

    public void RemoveLeash(Entity<LeashedComponent?> leashed, Entity<LeashComponent?> leash, bool breakJoint = true)
    {
        if (_net.IsClient || !Resolve(leashed, ref leashed.Comp))
            return;

        var jointId = leashed.Comp.JointId;
        leashed.Comp.JointId = null; // Just so future checks know that we deliberately removed the leash
        RemCompDeferred<LeashedComponent>(leashed); // Has to be deferred else the client explodes for some reason

        if (_container.TryGetContainer(leashed, LeashedComponent.VisualsContainerName, out var visualsContainer))
            _container.CleanContainer(visualsContainer);

        if (Resolve(leash, ref leash.Comp, false))
        {
            var leashedData = leash.Comp.Leashed.Where(it => it.JointId == jointId).ToList();
            foreach (var data in leashedData)
                leash.Comp.Leashed.Remove(data);
        }

        if (breakJoint && jointId is not null)
            _joints.RemoveJoint(leash, jointId);

        Dirty(leash);
    }

    /// <summary>
    ///     Sets the desired length of the leash. The actual length will be updated on the next physics tick.
    /// </summary>
    public void SetLeashLength(Entity<LeashComponent> leash, float length)
    {
        leash.Comp.Length = length;
        RefreshJoints(leash);
        _popups.PopupPredicted(Loc.GetString("leash-set-length-popup", ("length", length)), leash.Owner, null);

        // Wake all leashed entities up
        _physics.WakeBody(leash);
        foreach (var data in leash.Comp.Leashed)
            if (TryGetLeashTarget(GetEntity(data.Pulled), out var leashTarget))
                _physics.WakeBody(leashTarget);
    }

    #endregion
}
