using Content.Shared._Floof.Leash.Components;
using Content.Shared._Floof.Ropes.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    [Dependency] private readonly RopeSystem _ropes = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public static readonly string LeashJointIdPrefix = "leash-joint-";

    private List<(Entity<LeashComponent>, Entity<LeashedComponent>, Entity<LeashAnchorComponent>)> _pendingJointUpdates = new();

    private void InitializeJoints()
    {
        // SubscribeLocalEvent<LeashedComponent, JointAddedEvent>(OnJointAdded);
        // SubscribeLocalEvent<LeashedComponent, JointRemovedEvent>(OnJointRemoved, after: [typeof(SharedJointSystem)]);
    }

    // private void OnJointAdded(Entity<LeashedComponent> ent, ref JointAddedEvent args)
    // {
    //     // If we're on the client side, set the leash length to infinity to avoid predicting the leash
    //     if (_net.IsClient && args.Joint.ID.StartsWith(LeashJointIdPrefix) && args.Joint is DistanceJoint dj)
    //         dj.MaxLength = float.MaxValue;
    // }
    //
    // private void OnJointRemoved(Entity<LeashedComponent> ent, ref JointRemovedEvent args)
    // {
    //     // JointRemoved is called on both bodies, we only do this kinda check on the leashed
    //     var id = args.Joint.ID;
    //     if (_net.IsClient
    //         || ent.Comp.LifeStage >= ComponentLifeStage.Removing
    //         || GetEntity(ent.Comp.Leash) is not { } leashEnt
    //         || GetEntity(ent.Comp.Anchor) is not { } anchorEnt
    //         || ent.Comp.JointId != id
    //         || TerminatingOrDeleted(leashEnt)
    //         || !TryComp<LeashAnchorComponent>(anchorEnt, out var anchor)
    //         || !TryComp<LeashComponent>(leashEnt, out var leash))
    //         return;
    //
    //     _pendingJointUpdates.Add(((leashEnt, leash), ent, (anchorEnt, anchor)));
    // }

    // private void RefreshRelays(Entity<LeashComponent, TransformComponent> leash)
    // {
    //     // Server - ensure the holder of the leash is always correct
    //     // I do not know why, perhaps because RobustToolbox joint tooling is shitty,
    //     // but if the leash is inside a container that is inside another container (e.g. person inside a locker),
    //     // and then the middle container leaves the outer (person leaves the locker),
    //     // RobustToolbox won't update the joint between the leashed person and the leash (which should be relayed to the outer container - locker).
    //     // This means the person will stay attached to the outer container (locker).
    //     // To fix this, we force RT to update the joint relay
    //     if (TryComp<JointComponent>(leash, out var leashJointComp)
    //         && _container.TryGetOuterContainer(leash, leash.Comp2, out var jointRelayTarget)
    //         && leashJointComp.Relay != null
    //         && leashJointComp.Relay != jointRelayTarget.Owner)
    //         _joints.RefreshRelay(leash);
    //
    //     // Also do the same for all leashed entities
    //     foreach (var data in leash.Comp1.Leashed)
    //     {
    //         if (!TryGetEntity(data.Pulled, out var pulled) || !TryComp<LeashedComponent>(pulled, out var leashed))
    //             continue;
    //
    //         if (TryComp<JointComponent>(pulled, out var jointComp)
    //             && _container.TryGetOuterCo iner(pulled.Value, Transform(pulled.Value), out jointRelayTarget)
    //             && jointComp.Relay != null
    //             && jointComp.Relay != jointRelayTarget.Owner)
    //             _joints.RefreshRelay(pulled.Value);
    //     }
    // }

    /// <summary>
    ///     Removes all ropes on the leash and re-creates them.
    ///     If <paramref name="force"/> is false, only creates missing joints.
    /// </summary>
    public void RefreshRopes(Entity<LeashComponent> leash, bool force)
    {
        if (_net.IsClient)
            return;

        var config = leash.Comp.CurrentConfig;
        if (!_protoMan.Resolve(config.RopeConfig, out var ropeConfig))
            return;

        foreach (var data in leash.Comp.Leashed)
        {
            if (!TryGetEntity(data.Pulled, out var pulled)
                || !TryComp<LeashedComponent>(pulled, out var leashedComp)
                || !TryGetEntity(leashedComp.Anchor, out var anchor)
                || !TryComp<LeashAnchorComponent>(anchor, out var anchorComp))
                continue;

            if (force && TryGetEntity(data.Rope, out var rope))
                QueueDel(rope);

            if (_ropes.TryCreateRope(leash,
                    pulled,
                    ropeConfig,
                    config.Length,
                    out var newRope,
                    offsetRight: anchorComp.Offset)
               )
                data.Rope = GetNetEntity(newRope)!.Value;
        }
    }
}
