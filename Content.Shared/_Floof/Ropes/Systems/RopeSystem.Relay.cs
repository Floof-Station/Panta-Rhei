using System.Linq;
using Content.Shared._Floof.Ropes.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private void InitializeRelay()
    {
        SubscribeLocalEvent<RopeAttachedComponent, EntGotInsertedIntoContainerMessage>(OnAnchorInserted);
        SubscribeLocalEvent<RopeAttachedComponent, EntGotRemovedFromContainerMessage>(OnAnchorRemoved);
    }

    private void OnAnchorInserted(Entity<RopeAttachedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (_net.IsClient)
            return;

        // Note: we don't bother updating the rope as the container system will make sure to delete the joints that connect the links of the rope anyway
        // The event will be intercepted and the rope will be re-created if possible.
        foreach (var ropeInfo in ent.Comp.AttachedRopes.ToList())
        {
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
            // Remove & re-process (if needed) relay on the root anchor
            var root = (ent.Owner, ropeInfo);
            if (!TryResolveRootAnchor(ref root))
                continue;

            RemoveRelay(root.Owner, root.ropeInfo);
            ProcessRelay(root.Owner, root.ropeInfo);
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
}
