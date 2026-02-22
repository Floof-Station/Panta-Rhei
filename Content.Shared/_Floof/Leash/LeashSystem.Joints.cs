using Content.Shared._Floof.Leash.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    [Dependency] private readonly SharedJointSystem _joints = default!;

    public static readonly string LeashJointIdPrefix = "leash-joint-";

    private List<(Entity<LeashComponent>, Entity<LeashedComponent>, Entity<LeashAnchorComponent>)> _pendingJointUpdates = new();

    private void InitializeJoints()
    {
        SubscribeLocalEvent<LeashedComponent, JointAddedEvent>(OnJointAdded);
        SubscribeLocalEvent<LeashedComponent, JointRemovedEvent>(OnJointRemoved, after: [typeof(SharedJointSystem)]);
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

    private void RefreshRelays(Entity<LeashComponent, TransformComponent> leash)
    {
        if (!ShouldPredictLeashes())
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
            if (!TryGetEntity(data.Pulled, out var pulled) || !TryComp<LeashedComponent>(pulled, out var leashed))
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
        var canRestore = !TerminatingOrDeleted(leash) && !TerminatingOrDeleted(leashed) &&
                         !TerminatingOrDeleted(anchor);
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
        var length = Transform(leashTarget)
            .Coordinates.TryDistance(EntityManager, Transform(leash).Coordinates, out var dist)
            ? MathF.Max(dist, leash.Comp.Length)
            : leash.Comp.Length;

        joint.MinLength = 0f;
        joint.MaxLength = length;
        joint.Stiffness = 0f;
        joint.CollideConnected = true; // This is just for performance reasons and doesn't actually make mobs collide.
        joint.Damping = 0f;

        return joint;
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
            if (!TryGetEntity(data.Pulled, out var pulled) || !TryComp<LeashedComponent>(pulled, out var leashed))
                continue;

            var shouldExist = CanCreateJoint(pulled.Value, leash);
            var exists = data.JointId != null;

            if (exists && !shouldExist && TryComp<JointComponent>(pulled, out var jointComp) &&
                jointComp.GetJoints.TryGetValue(data.JointId!, out var joint))
            {
                data.JointId = leashed.JointId = null;
                _joints.RemoveJoint(joint);

                Log.Debug($"Removed obsolete leash joint between {leash.Owner} and {pulled.Value}");
            }
            else if (!exists && shouldExist)
            {
                var jointId = $"leash-joint-{data.Pulled}";
                joint = CreateLeashJoint(jointId, leash, pulled.Value);
                data.JointId = leashed.JointId = jointId;

                Log.Debug($"Added new leash joint between {leash.Owner} and {pulled.Value}");
            }
        }
    }
}
