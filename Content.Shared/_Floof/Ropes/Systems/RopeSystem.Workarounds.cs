using Content.Shared._Floof.Ropes.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Physics;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    private void InitializeWorkarounds()
    {
        SubscribeLocalEvent<RopeAttachedComponent, BeforeTeleportedEvent>(OnBeforeTeleported);
        SubscribeLocalEvent<RopeAttachedComponent, TeleportedEvent>(OnTeleported);
    }

    private void OnBeforeTeleported(Entity<RopeAttachedComponent> ent, ref BeforeTeleportedEvent args)
    {
        foreach (var ropeInfo in ent.Comp.AttachedRopes)
        {
            if (CanTeleportRope(ropeInfo.Rope, out var reason))
                continue;

            args.CancelReason = reason;
            args.Cancel();
            return;
        }
    }

    private void OnTeleported(Entity<RopeAttachedComponent> teleported, ref TeleportedEvent args)
    {
        // Try to pull all attached entities in and pray for the best.
        var toTeleport = new List<(Entity<RopeComponent> rope, EntityUid mob)>();
        foreach (var ropeInfo in teleported.Comp.AttachedRopes)
        {
            if (!_ropeQuery.TryComp(ropeInfo.Rope, out var ropeComp))
                continue;

            if (ropeComp.IsDisabled)
                continue; // entities are inside the same container or something, we shouldn't mess with that

            if (ropeComp.ConnectedStart?.Anchor is {} left)
                toTeleport.Add(((ropeInfo.Rope, ropeComp), left));
            if (ropeComp.ConnectedEnd?.Anchor is {} right)
                toTeleport.Add(((ropeInfo.Rope, ropeComp), right));
        }

        var teleportedCoords = Transform(teleported.Owner).Coordinates;
        foreach (var (rope, otherEnt) in toTeleport)
        {
            // Don't teleport entities that are too close to the one that has just teleported (usually that's the one that's teleported)
            if (GetEffectiveDistance(teleported, otherEnt) < rope.Comp.RopeLength)
                continue;

            // Always teleport the outer container in case the other attached entity is e.g. a locker
            var otherSubject = otherEnt;
            if (_containers.TryGetOuterContainer(otherSubject, Transform(otherSubject), out var outerContainer))
                otherSubject = outerContainer.Owner;

            // Also avoid teleporting if it's static (e.g. an anchored leash)
            if (!_physicsQuery.TryComp(otherSubject, out var otherPhysics)
                || otherPhysics.BodyType == BodyType.Static
                || !otherPhysics.CanCollide)
            {
                continue;
            }

            EnsureComp<PortalTimeoutComponent>(otherSubject).EnteredPortal = args.Portal; // So it doesn't get instantly teleported back

            _xform.SetCoordinates(otherSubject, teleportedCoords);

            // At least one of the anchors has to have teleported, and we have set the position of the other, so it should be fine
            // If links == 0, it's a snowflake case that's handled in BeforeTeleport
            if (rope.Comp.Links.Count > 0)
                DistributeLinksBetweenAnchors(rope);

            Log.Info($"Teleporting {ToPrettyString(otherEnt)} to follow the teleportation of {teleported}.");
        }
    }

    private bool CanTeleportRope(Entity<RopeComponent?> rope, out string? reason)
    {
        reason = null;
        if (!_ropeQuery.Resolve(rope, ref rope.Comp))
            return true;

        // I don't even know man.
        if (rope.Comp.Links.Count > 0 && !rope.Comp.IsDisabled)
        {
            reason = Loc.GetString("rope-portal-fail-too-few-links");
            return false;
        }

        return true;
    }
}
