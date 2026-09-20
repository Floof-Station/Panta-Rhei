using System.Linq;
using System.Numerics;
using Content.Shared._Floof.Ropes.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    // If the distance between two entities is x, then a rope of length AT LEAST (x - tolerance) can be created between them
    private float _connectionDstTolerance = 2;

    private void InitializeRelay()
    {
        SubscribeLocalEvent<RopeAttachedComponent, EntityTerminatingEvent>(OnAnchorShutdown);
        SubscribeLocalEvent<RopeAttachedComponent, EntGotInsertedIntoContainerMessage>(OnAnchorInserted);
        SubscribeLocalEvent<RopeAttachedComponent, EntGotRemovedFromContainerMessage>(OnAnchorRemoved);
    }

    private void OnAnchorShutdown(Entity<RopeAttachedComponent> ent, ref EntityTerminatingEvent args)
    {
        foreach (var rope in ent.Comp.AttachedRopes.ToList())
        {
            if (!_ropeQuery.TryComp(rope.Rope, out var ropeComp) || TerminatingOrDeleted(rope.Rope))
                continue;

            // Try detaching the rope. This code duplication is turning into spaghetti.
            if (ropeComp.ConnectedStart?.Anchor == ent)
                TryDetachStart((rope.Rope, ropeComp));
            if (ropeComp.ConnectedEnd?.Anchor == ent)
                TryDetachEnd((rope.Rope, ropeComp));
        }
    }

    private void OnAnchorInserted(Entity<RopeAttachedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (_net.IsClient)
            return;

        // Note: we don't bother updating the rope as the container system will make sure to delete the joints that connect the links of the rope anyway
        // The event will be intercepted and the rope will be re-created if possible.
        foreach (var ropeInfo in ent.Comp.AttachedRopes.ToList())
        {
            if (!_ropeQuery.TryComp(ropeInfo.Rope, out var ropeComp))
                continue;

            QueueUpdate(ropeInfo.Rope);

            // Only bother relaying if the rope is enabled. If it's in someone backpack, it's pointless
            if (ropeComp.IsDisabled)
                continue;

            // Refresh relays on each root anchor (which might or might not be this entity)
            var root = (ent.Owner, ropeInfo);
            if (!TryResolveRootAnchor(ref root))
                continue;

            ProcessRelay(root.Owner, root.ropeInfo);
        }
    }

    private void OnAnchorRemoved(Entity<RopeAttachedComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_net.IsClient)
            return;

        foreach (var ropeInfo in ent.Comp.AttachedRopes.ToList())
        {
            QueueUpdate(ropeInfo.Rope);

            // Remove & re-process (if needed) relay on the root anchor
            var root = (ent.Owner, ropeInfo);
            if (!TryResolveRootAnchor(ref root))
                continue;

            // In this case we DO process relays even if the rope is disabled as this could mean that the rope was moved between the person's backpack and inventory or something
            RemoveRelay(root.Owner, root.ropeInfo);
            ProcessRelay(root.Owner, root.ropeInfo);
        }
    }

    /// <summary>
    ///     This is to be called whenever the rope is attached to a new anchor.
    /// </summary>
    private void OnRopeAttached(Entity<RopeComponent?> rope, EntityUid connector)
    {
        if (!_ropeAttachedQuery.TryComp(connector, out var ropeAttachedComp))
            ropeAttachedComp = AddComp<RopeAttachedComponent>(connector);

        var args = new RopeAttachedComponent.AttachedRopeInfo(rope, null, null);
        if (ropeAttachedComp.IndexOfRope(rope) == -1)
            ropeAttachedComp.AttachedRopes.Add(args);

        ProcessRelay(connector, args);
    }

    /// <summary>
    ///     This is to be called whenever the rope is detached from an old anchor.
    /// </summary>
    private void OnRopeDetached(Entity<RopeComponent?> rope, EntityUid connector)
    {
        if (!_ropeAttachedQuery.TryComp(connector, out var ropeAttachedComp))
            return;

        for (var i = ropeAttachedComp.AttachedRopes.Count - 1; i >= 0; i--)
        {
            var ropeInfo = ropeAttachedComp.AttachedRopes[i];
            if (ropeInfo.Rope != rope.Owner)
                continue;

            ropeAttachedComp.AttachedRopes.RemoveSwap(i);
            RemoveRelay(connector, ropeInfo);
        }
    }

    /// <summary>
    ///     Finds the root anchor and attachment info for the given pair of (relayed anchor, relayed attachment info).
    ///     Only updates the value behind the reference if it's a relay.
    /// </summary>
    private bool TryResolveRootAnchor(ref (EntityUid, RopeAttachedComponent.AttachedRopeInfo) current)
    {
        if (current.Item2.RelayedFrom == null)
            return true;

        var root = current.Item2.RelayedFrom;
        if (!_ropeAttachedQuery.TryComp(root, out var rootAttachedComp)
            || (rootAttachedComp.IndexOfRope(current.Item2.Rope) is { } rootIndex && rootIndex == -1))
            return false;

        current = (root.Value, rootAttachedComp.AttachedRopes[rootIndex]);
        return true;
    }

    /// <summary>
    ///     This method is to be called whenever a connector attached to a rope needs its relay processed (added if needed).
    ///     The connector should be the relay SOURCE.
    /// </summary>
    private void ProcessRelay(EntityUid connector, RopeAttachedComponent.AttachedRopeInfo ropeInfo)
    {
        DebugTools.Assert(ropeInfo.RelayedFrom != null, "Cannot relay an already relayed rope attachment");

        if (ropeInfo.RelayedTo != null)
        {
            Log.Warning("Rope already has a relay, removing the old one.");
            RemoveRelay(connector, ropeInfo);
        }

        // We add the rope relay to all containers that (implicitly or explicitly) contain this entity
        ropeInfo.RelayedTo = new(5);
        var toProcess = connector;
        while (_containers.TryGetContainingContainer(toProcess, out var container))
        {
            var relayTarget = container.Owner;
            if (!_ropeAttachedQuery.TryGetComponent(relayTarget, out var relayComp))
                relayComp = AddComp<RopeAttachedComponent>(relayTarget);

            DebugTools.Assert(relayComp.IndexOfRope(ropeInfo.Rope) == -1);

            // We make sure the relay's AttachedRopes list doesn't already contain an entry for this one
            // because it's possible that its owner is the same entity the rope is attached to
            if (!ropeInfo.RelayedTo.Contains(relayTarget) && relayComp.IndexOfRope(ropeInfo.Rope) == -1)
            {
                ropeInfo.RelayedTo.Add(relayTarget);
                relayComp.AttachedRopes.Add(new(ropeInfo.Rope, null, connector));
            }

            toProcess = container.Owner;
        }
    }

    /// <summary>
    ///     This method is to be called whenever a connector attached to a rope needs to have its relay removed.
    ///     The connector should be the relay SOURCE.
    /// </summary>
    private void RemoveRelay(EntityUid connector, RopeAttachedComponent.AttachedRopeInfo ropeInfo)
    {
        // This only removes the component from the relay target, callers are expected to handle the source
        if (ropeInfo.RelayedTo is not {} relayTargets)
            return;

        foreach (var relayTarget in relayTargets)
            RemoveRelayTarget(relayTarget, ropeInfo.Rope);
        ropeInfo.RelayedTo = null;

        // Consider the following situation.
        // Entity C is connected to a rope. C is contained inside B, B is contained inside A (C might be a leash, B might be a player character, A might be a locker)
        // In this situation, C is the joint relay of A.
        // If B leaves A without removing C from itself (might be that the player opened the locker), RT won't refresh joint relays on C.
        // This is because since C is TECHNICALLY not inside A, EntGotRemovedFromContainer is not raised on it, which is what the joint system listens on.
        // So we need to do that manually here.
        //
        // It's also why we need to relay rope attachments to all parents
        // - so we can detect when one of the entities containing the rope anchor leaves the outer container
        //
        // I wish I could fix this with an engine change, but joint relays are barely used,
        // and making container events be relayed to all implicit children would probably have a noticeable performance impact
        _joints.RefreshRelay(connector);
    }

    private void RemoveRelayTarget(EntityUid relayTarget, EntityUid rope)
    {
        if (!_ropeAttachedQuery.TryComp(relayTarget, out var relayComp)
            || (relayComp.IndexOfRope(rope) is var index && index == -1))
            return;

        relayComp.AttachedRopes.RemoveAt(index);
        if (relayComp.AttachedRopes.Count == 0)
            RemComp(relayTarget, relayComp);
    }

    #region public API

    public RopeComponent.AnchorInfo? GetAnchor(Entity<RopeComponent> rope, RopeSide side) => side switch
    {
        RopeSide.Start => rope.Comp.ConnectedStart,
        RopeSide.End => rope.Comp.ConnectedEnd,
        _ => throw new ArgumentOutOfRangeException()
    };

    /// <summary>
    ///     Connects the specified side of the rope to the specified anchor.
    ///     If the rope has no links, this method will have no effect.
    /// </summary>
    public bool TryConnectRopeSide(Entity<RopeComponent?> rope, EntityUid connector, RopeSide side, Vector2 offset = default)
    {
        // Note: we don't allow attaching the same side to another entity unless the rope is currently disabled (jointId == invalidJointMarker)
        // In case the rope is disabled, this call could be from EnableRope, in which case we want to force it
        // Ideally this should be made into a `force` parameter, but idc at this point
        if (!Resolve(rope, ref rope.Comp)
            || GetAnchor(rope!, side) is {} existing && existing.JointId != _invalidJointMarker)
            return false;

        if (rope.Comp.Links.Count == 0)
        {
            Log.Error("Cannot attach a rope with 0 links. Specify anchors in TryCreateRope!");
            return false;
        }

        var closestLink = side switch
        {
            RopeSide.Start => rope.Comp.Links[0],
            RopeSide.End => rope.Comp.Links[^1],
            _ => throw new ArgumentOutOfRangeException(),
        };
        // Not doing this as it can lead to failures when creating attachments on a disabled rope... which is another edge case
        // var dist = GetEffectiveDistance(connector, closestLink.LinkEntity);
        // if (float.IsInfinity(dist))
        //     return false;

        // Create a distance joint
        var joint = rope.Comp.IsDisabled ? null : CreateDistanceJoint(connector, closestLink.LinkEntity, rope.Comp, offset);
        var jointId = joint?.ID ?? _invalidJointMarker;
        switch (side)
        {
            case RopeSide.Start:
                rope.Comp.ConnectedStart = new(connector, jointId, offset);
                closestLink.LeftJoint = jointId;
                break;
            case RopeSide.End:
                rope.Comp.ConnectedEnd = new(connector, jointId, offset);
                closestLink.RightJoint = jointId;
                break;
        }

        // We don't inform the system about the attachment if the rope is disabled. This will be handled in Enable()
        if (!rope.Comp.IsDisabled)
            OnRopeAttached(rope, connector);
        Dirty(rope, rope.Comp);
        return true;
    }

    /// <see cref="TryConnectRopeSide"/>
    public bool TryConnectRopeStart(Entity<RopeComponent?> rope, EntityUid connector, Vector2 offset = default) =>
        TryConnectRopeSide(rope, connector, RopeSide.Start, offset);

    /// <see cref="TryConnectRopeSide"/>
    public bool TryConnectRopeEnd(Entity<RopeComponent?> rope, EntityUid connector, Vector2 offset = default) =>
        TryConnectRopeSide(rope, connector, RopeSide.End, offset);

    /// <summary>
    ///     Detaches the given side of the rope.
    ///     If the rope has no links, this method will have no effect.
    /// </summary>
    public bool TryDetachRopeSide(Entity<RopeComponent?> rope, RopeSide side)
    {
        if (!Resolve(rope, ref rope.Comp) || GetAnchor(rope!, side) is not {} anchor)
            return false;

        if (rope.Comp.Links.Count == 0)
        {
            Log.Error("Cannot detach a rope with 0 links. Delete the rope entity instead!");
            return false;
        }

        var closestLink = side switch
        {
            RopeSide.Start => rope.Comp.Links[0],
            RopeSide.End => rope.Comp.Links[^1],
            _ => throw new ArgumentOutOfRangeException(),
        };
        _joints.RemoveJoint(closestLink.LinkEntity, anchor.JointId);

        switch (side)
        {
            case RopeSide.Start:
                rope.Comp.ConnectedStart = null;
                closestLink.LeftJoint = null;
                break;
            case RopeSide.End:
                rope.Comp.ConnectedEnd = null;
                closestLink.RightJoint = null;
                break;
        }

        // We need to inform the system about the detachment even if the rope is disabled, in case there's something important going on.
        OnRopeDetached(rope, anchor.Anchor);
        Dirty(rope, rope.Comp);
        return true;
    }

    /// <see cref="TryDetachRopeSide"/>
    public bool TryDetachStart(Entity<RopeComponent?> rope) =>
        TryDetachRopeSide(rope, RopeSide.Start);

    /// <see cref="TryDetachRopeSide"/>
    public bool TryDetachEnd(Entity<RopeComponent?> rope) =>
        TryDetachRopeSide(rope, RopeSide.End);

    #endregion
}
