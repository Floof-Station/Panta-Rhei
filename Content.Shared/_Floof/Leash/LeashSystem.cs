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
// It should be split into client and server counterparts
public sealed class LeashSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfters = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public static readonly VerbCategory LeashLengthConfigurationCategory =
        new("verb-categories-leash-config", "/Textures/_Floof/Interface/VerbIcons/resize.svg.192dpi.png");
    public static readonly string LeashJointIdPrefix = "leash-joint-";

    private List<(Entity<LeashComponent>, Entity<LeashedComponent>, Entity<LeashAnchorComponent>)> _pendingJointUpdates = new();

    #region Lifecycle

    public override void Initialize()
    {
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));

        SubscribeLocalEvent<LeashAnchorComponent, BeingUnequippedAttemptEvent>(OnAnchorUnequipping);
        SubscribeLocalEvent<LeashAnchorComponent, GetVerbsEvent<EquipmentVerb>>(OnGetEquipmentVerbs);
        SubscribeLocalEvent<LeashedComponent, JointAddedEvent>(OnJointAdded);
        SubscribeLocalEvent<LeashedComponent, JointRemovedEvent>(OnJointRemoved, after: [typeof(SharedJointSystem)]);
        SubscribeLocalEvent<LeashedComponent, GetVerbsEvent<InteractionVerb>>(OnGetLeashedVerbs);
        SubscribeLocalEvent<LeashedComponent, EntGotInsertedIntoContainerMessage>(OnLeashedContainerChanged);
        SubscribeLocalEvent<LeashedComponent, EntGotRemovedFromContainerMessage>(OnLeashedContainerChanged);

        SubscribeLocalEvent<LeashComponent, ExaminedEvent>(OnLeashExamined);
        SubscribeLocalEvent<LeashComponent, EntGotInsertedIntoContainerMessage>(OnLeashContainerChanged);
        SubscribeLocalEvent<LeashComponent, EntGotRemovedFromContainerMessage>(OnLeashContainerChanged);
        SubscribeLocalEvent<LeashComponent, GetVerbsEvent<AlternativeVerb>>(OnGetLeashVerbs);

        SubscribeLocalEvent<LeashAnchorComponent, LeashAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<LeashedComponent, LeashDetachDoAfterEvent>(OnDetachDoAfter);

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
        if (data.Anchor == NetEntity.Invalid || !TryGetEntity(data.Anchor, out var target))
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
            if (joint is not null)
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

    private void RefreshRelays(Entity<LeashComponent, TransformComponent> leash)
    {
        if (!_net.IsServer)
            return;

        // Server - ensure the holder of the leash is always correct
        // I do not know why, perhaps because RobustToolbox joint tooling is shitty,
        // but if the leash is inside a container that is inside another container (e.g. person inside a locker),
        // and then the middle container leaves the outer (person leaves the locker),
        // RobustToolbox won't update the joint between the leashed person and the leash (which should be relayed to the outer container - locker).
        // This means the person will stay attached to the outer container (locker).
        // To fix this, we force RT to update the joint relay
        if (TryComp<JointComponent>(leash, out var leashJointComp)
            && _container.TryGetOuterContainer(leash, leash.Comp2, out var jointRelayTarget)
            && leashJointComp.Relay != null
            && leashJointComp.Relay != jointRelayTarget.Owner)
            _joints.RefreshRelay(leash);

        // Also do the same for all leashed entities
        foreach (var data in leash.Comp1.Leashed)
        {
            if (!TryGetEntity(data.Anchor, out var pulled) || !TryComp<LeashedComponent>(pulled, out var leashed))
                continue;

            if (TryComp<JointComponent>(pulled, out var jointComp)
                && _container.TryGetOuterContainer(pulled.Value, Transform(pulled.Value), out jointRelayTarget)
                && jointComp.Relay != null
                && jointComp.Relay != jointRelayTarget.Owner)
                _joints.RefreshRelay(pulled.Value);
        }
    }

    private void ProcessPendingJointUpdate(Entity<LeashComponent> leash,
        Entity<LeashedComponent> leashed,
        Entity<LeashAnchorComponent> anchor)
    {
        var canRestore = !TerminatingOrDeleted(leash) && !TerminatingOrDeleted(leashed) && !TerminatingOrDeleted(anchor);
        if (canRestore)
        {
            var leashXform = Transform(leash);
            var leashedXform = Transform(leashed);
            canRestore &= leashXform.MapUid == leashedXform.MapUid
                          && leashXform.Coordinates.TryDistance(EntityManager, leashedXform.Coordinates, out var dst)
                          && dst <= leash.Comp.MaxDistance;
            // The anchor must be either the entity itself or something parented to them (clothing)
            canRestore &= anchor.Owner == leashed.Owner || _container.ContainsEntity(leashed, anchor);
        }

        RemoveLeash(leashed!, leash!, false);
        if (canRestore)
            DoLeash(anchor, leash, leashed, true);
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

    private void OnGetLeashedVerbs(Entity<LeashedComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || GetEntity(ent.Comp.Leash) is not { } leash
            || !TryComp<LeashComponent>(leash, out var leashComp))
            return;

        var user = args.User;
        args.Verbs.Add(new()
        {
            Text = Loc.GetString("verb-unleash-text"),
            Act = () => TryUnleash(ent.Owner, (leash, leashComp), user)
        });
    }

    private void OnGetLeashVerbs(Entity<LeashComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || ent.Comp.LengthConfigs is not { } configurations
            || !CanInteractWithLeash(args.User, ent))
            return;

        // Add a menu listing each length configuration.
        foreach (var length in configurations)
        {
            args.Verbs.Add(new()
            {
                Text = Loc.GetString("verb-leash-set-length-text", ("length", length)),
                Act = () => SetLeashLength(ent, length),
                Category = LeashLengthConfigurationCategory
            });
        }
    }

    private void OnJointAdded(Entity<LeashedComponent> ent, ref JointAddedEvent args)
    {
        // If we're on the client side, set the leash length to infinity to avoid predicting the leash
        if (_net.IsClient && args.Joint.ID.StartsWith(LeashJointIdPrefix) && args.Joint is DistanceJoint dj)
            dj.MaxLength = float.MaxValue;
    }

    private void OnJointRemoved(Entity<LeashedComponent> ent, ref JointRemovedEvent args)
    {
        // JointRemoved is called on both bodies, we only do this kinda check on the leashed
        var id = args.Joint.ID;
        if (_net.IsClient
            || ent.Comp.LifeStage >= ComponentLifeStage.Removing
            || GetEntity(ent.Comp.Leash) is not { } leashEnt
            || GetEntity(ent.Comp.Anchor) is not { } anchorEnt
            || ent.Comp.JointId != id
            || TerminatingOrDeleted(leashEnt)
            || !TryComp<LeashAnchorComponent>(anchorEnt, out var anchor)
            || !TryComp<LeashComponent>(leashEnt, out var leash))
            return;

        _pendingJointUpdates.Add(((leashEnt, leash), ent, (anchorEnt, anchor)));
    }

    private void OnLeashedContainerChanged(EntityUid ent, LeashedComponent comp, ContainerModifiedMessage args)
    {
        // Note: we can't use the Entity<T> handler here because it doesn't support polymorphism
        if (!_net.IsClient && GetEntity(comp.Leash) is { } leashEnt && TryComp<LeashComponent>(leashEnt, out var leash))
            RefreshJoints((leashEnt, leash));
    }

    private void OnLeashExamined(Entity<LeashComponent> ent, ref ExaminedEvent args)
    {
        var length = ent.Comp.Length;
        args.PushMarkup(Loc.GetString("leash-length-examine-text", ("length", length)));
    }

    private void OnLeashContainerChanged(EntityUid ent, LeashComponent comp, ContainerModifiedMessage args)
    {
        // Note: we can't use the Entity<T> handler here because it doesn't support polymorphism
        if (!_net.IsClient)
            RefreshJoints((ent, comp));
    }

    private void OnAttachDoAfter(Entity<LeashAnchorComponent> ent, ref LeashAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled
            || !TryComp<LeashComponent>(args.Used, out var leash)
            || !CanLeash(ent, (args.Used.Value, leash)))
            return;

        DoLeash(ent, (args.Used.Value, leash), EntityUid.Invalid);
    }

    private void OnDetachDoAfter(Entity<LeashedComponent> ent, ref LeashDetachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || GetEntity(ent.Comp.Leash) is not { } leash)
            return;

        RemoveLeash(ent!, leash);
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
            .Select(it => GetEntity(it.Anchor))
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
    ///     Tries to find the entity that gets leashed for the given anchor entity.
    /// </summary>
    private bool TryGetLeashTarget(Entity<LeashAnchorComponent?> ent, out EntityUid leashTarget)
    {
        leashTarget = default;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Kind.HasFlag(LeashAnchorComponent.AnchorKind.Clothing)
            && TryComp<ClothingComponent>(ent, out var clothing)
            && clothing.InSlot != null
            && _container.TryGetContainingContainer(ent.Owner, out var container))
        {
            leashTarget = container.Owner;
            return true;
        }

        if (ent.Comp.Kind.HasFlag(LeashAnchorComponent.AnchorKind.Intrinsic))
        {
            leashTarget = ent.Owner;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Returns true if a leash joint can be created between the two specified entities.
    ///     This will return false if one of the entities is a parent of another, or if the entities are on different maps.
    /// </summary>
    public bool CanCreateJoint(EntityUid a, EntityUid b)
    {
        BaseContainer? aOuter = null, bOuter = null;

        // Unless the entities are inside the same container, it should be safe to create a joint
        var aXform = Transform(a);
        var bXform = Transform(b);

        if (aXform.MapUid != bXform.MapUid)
            return false;

        if (!_container.TryGetOuterContainer(a, aXform, out aOuter)
            && !_container.TryGetOuterContainer(b, bXform, out bOuter))
            return true;

        // Otherwise, we need to make sure that neither of the entities contain the other, and that they are not in the same container.
        return a != bOuter?.Owner && b != aOuter?.Owner && aOuter?.Owner != bOuter?.Owner;
    }

    private DistanceJoint CreateLeashJoint(string jointId, Entity<LeashComponent> leash, EntityUid leashTarget)
    {
        var joint = _joints.CreateDistanceJoint(leash, leashTarget, id: jointId);
        // If the soon-to-be-leashed entity is too far away, we don't force it any closer.
        // The system will automatically reduce the length of the leash once it gets closer.
        var length = Transform(leashTarget).Coordinates.TryDistance(EntityManager, Transform(leash).Coordinates, out var dist)
            ? MathF.Max(dist, leash.Comp.Length)
            : leash.Comp.Length;

        joint.MinLength = 0f;
        joint.MaxLength = length;
        joint.Stiffness = 1f;
        joint.CollideConnected = true; // This is just for performance reasons and doesn't actually make mobs collide.
        joint.Damping = 1f;

        return joint;
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

    #endregion

    #region public api

    public bool CanLeash(Entity<LeashAnchorComponent> anchor, Entity<LeashComponent> leash)
    {
        // Note: we don't actually care if there's a joint - that thing can be missing if CanCreateJoint is false.
        return leash.Comp.Leashed.Count < leash.Comp.MaxJoints
            && GetLeashed(anchor).Comp?.Leash == null
            && Transform(anchor).Coordinates.TryDistance(EntityManager, Transform(leash).Coordinates, out var dst)
            && dst <= leash.Comp.Length;
    }

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
        foreach (var data in leash.Comp.Leashed)
            if (TryGetLeashTarget(GetEntity(data.Anchor), out var leashTarget))
                _physics.WakeBody(leashTarget);
    }

    /// <summary>
    ///     Refreshes all joints for the specified leash.
    ///     This will remove all obsolete joints, such as those for which CanCreateJoint returns false,
    ///     and re-add all joints that were previously removed for the same reason, but became valid later.
    /// </summary>
    public void RefreshJoints(Entity<LeashComponent> leash)
    {
        foreach (var data in leash.Comp.Leashed)
        {
            if (!TryGetEntity(data.Anchor, out var pulled) || !TryComp<LeashedComponent>(pulled, out var leashed))
                continue;

            var shouldExist = CanCreateJoint(pulled.Value, leash);
            var exists = data.JointId != null;

            if (exists && !shouldExist && TryComp<JointComponent>(pulled, out var jointComp) && jointComp.GetJoints.TryGetValue(data.JointId!, out var joint))
            {
                data.JointId = leashed.JointId = null;
                _joints.RemoveJoint(joint);

                Log.Debug($"Removed obsolete leash joint between {leash.Owner} and {pulled.Value}");
            }
            else if (!exists && shouldExist)
            {
                var jointId = $"leash-joint-{data.Anchor}";
                joint = CreateLeashJoint(jointId, leash, pulled.Value);
                data.JointId = leashed.JointId = jointId;

                Log.Debug($"Added new leash joint between {leash.Owner} and {pulled.Value}");
            }
        }
    }

    #endregion
}
