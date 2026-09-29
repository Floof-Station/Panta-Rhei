using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._Floof.Ropes.Components;
using Content.Shared._Floof.Ropes.Events;
using Content.Shared._Floof.Ropes.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    /// <summary>
    ///     Creates a rope between the two entities. Returns the rope data entity. By default, the data entity is attached to a middle link (or left anchor if 0-link).
    ///     Callers are advised to move it to an appropriate spot.
    ///
    ///     If rope anchor is null, creates a rope at the anchor's position and does nothing else.
    /// </summary>
    public bool TryCreateRope(
        EntityUid leftAnchor,
        EntityUid? rightAnchor,
        RopeConfigurationPrototype config,
        float length,
        [NotNullWhen(true)] out Entity<RopeComponent>? createdRope,
        Vector2 offsetLeft = default,
        Vector2 offsetRight = default)
    {
        var leftXform = Transform(leftAnchor);
        if (rightAnchor != null && !CanRopeExistBetween(leftAnchor, rightAnchor.Value, length, leftXform)
            || !_xform.TryGetMapOrGridCoordinates(leftAnchor, out var leftCoords, leftXform)) // We must make sure the links are spawned on ground
        {
            createdRope = null;
            return false;
        }

        var leftMapCoords = _xform.ToMapCoordinates(leftXform.Coordinates);
        var rope = CreateRopeEntityUninitialized(config, length, leftMapCoords);
        createdRope = rope;

        rope.Comp.ConnectedStart = new(leftAnchor, _invalidJointMarker, offsetLeft);
        if (rightAnchor != null)
            rope.Comp.ConnectedEnd = new(rightAnchor.Value, _invalidJointMarker, offsetRight);

        UpdateRope(rope);

        // Make the data entity a child of either a middle link or the left anchor
        var linkCount = rope.Comp.Links.Count;
        var dataHolder = linkCount > 0 ? rope.Comp.Links[linkCount / 2].LinkEntity : leftAnchor;
        _xform.SetCoordinates(rope, new(dataHolder, Vector2.Zero));

        // Dirtying shouldn't be necessary since the rope has just been created
        return true;
    }

    /// <see cref="TryCreateRope(EntityUid,EntityUid?,RopeConfigurationPrototype,float,out Entity{RopeComponent}?,Vector2,Vector2)"/>
    public bool TryCreateRope(
        EntityUid leftAnchor,
        EntityUid? rightAnchor,
        ProtoId<RopeConfigurationPrototype> config,
        float length,
        [NotNullWhen(true)] out Entity<RopeComponent>? createdRope,
        Vector2 offsetLeft = default,
        Vector2 offsetRight = default)
    {
        if (!_protoMan.Resolve(config, out var prototype))
        {
            createdRope = null;
            return false;
        }

        return TryCreateRope(leftAnchor, rightAnchor, prototype, length, out createdRope, offsetLeft, offsetRight);
    }

    public bool CanRopeExistBetween(EntityUid left, EntityUid right, float length, TransformComponent? leftXform = null, TransformComponent? rightXform = null)
    {
        leftXform ??= Transform(left);
        rightXform ??= Transform(right);
        // Can't joint entities on different maps.
        if (leftXform.MapID != rightXform.MapID)
            return false;

        if (GetEffectiveDistance(leftXform, rightXform) > length + _connectionDstTolerance)
        {
            Log.Warning($"Refusing to create a rope shorter than the distance between the two entities: {ToPrettyString(left)}, {ToPrettyString(right)}");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Checks if the rope should be *temporarily* disabled.
    /// </summary>
    public bool ShouldTemporarilyDisableRope(EntityUid left, EntityUid right, TransformComponent? leftXform = null, TransformComponent? rightXform = null)
    {
        leftXform ??= Transform(left);
        rightXform ??= Transform(right);

        // If the two entities are in the same container... sucks
        BaseContainer? leftContainer = null, rightContainer = null;
        _containers.TryGetOuterContainer(left, leftXform, out leftContainer);
        _containers.TryGetOuterContainer(right, rightXform, out rightContainer);
        if (leftContainer != null && leftContainer.Owner == rightContainer?.Owner)
            return true;

        // Or if one of them directly or indirectly contains the other
        if (_xform.ContainsEntity(left, (right, rightXform))
            || _xform.ContainsEntity(right, (left, leftXform)))
            return true;

        // Or if the outer container of either is a non-physics entity
        if (leftContainer != null
            && (!_physicsQuery.TryComp(leftContainer?.Owner, out var leftContainerPhysics) || !leftContainerPhysics.CanCollide))
            return true;

        if (rightContainer != null
            && (!_physicsQuery.TryComp(rightContainer?.Owner, out var rightContainerPhysics) || !rightContainerPhysics.CanCollide))
            return true;

        return false;
    }

    /// <summary>
    ///     Repositions all rope links so that they uniformly distributed between the two anchors of the rope.
    ///     Does nothing if the rope lacks one or more of the anchors.
    ///
    ///     <p>If <paramref name="arcApproximation"/> is true,
    ///     distributes the points on an approximated arc so that the length of the rope is roughly equal to its ideal length.</p>
    /// </summary>
    public void DistributeLinksBetweenAnchors(Entity<RopeComponent?> rope, bool arcApproximation = true)
    {
        if (!Resolve(rope, ref rope.Comp))
            return;

        if (rope.Comp.ConnectedStart is not { } left || rope.Comp.ConnectedEnd is not { } right)
            return;

        DistributeLinksBetween(left.Anchor, right.Anchor, rope!, arcApproximation);
    }

    /// <summary>
    ///     Repositions all rope links so that they uniformly distributed between the two specified entities.
    ///
    ///     <p>If <paramref name="arcApproximation"/> is true,
    ///     distributes the points on an approximated arc so that the length of the rope is roughly equal to its ideal length.</p>
    /// </summary>
    private void DistributeLinksBetween(EntityUid leftAnchor, EntityUid rightAnchor, Entity<RopeComponent> rope, bool arcApproximation = true)
    {
        if (rope.Comp.Links.Count == 0)
            return;

        // Get world positions of the two anchors
        var leftXform = Transform(leftAnchor);
        var rightXform = Transform(rightAnchor);
        var map = leftXform.MapID;
        if (leftXform.MapID != rightXform.MapID)
        {
            Log.Error($"Cannot distribute joints between {ToPrettyString(leftAnchor)} and {ToPrettyString(rightAnchor)} as they are on different maps.");
            return;
        }

        var leftPos = _xform.GetWorldPosition(leftXform);
        var rightPos = _xform.GetWorldPosition(rightXform);
        // If leftPos == rightPos, the direction vector becomes nan
        var direction = leftPos != rightPos ? (rightPos - leftPos).Normalized() : Vector2.Zero;
        var distance = direction.Length();

        // If the distance between entities is much lower than rope length, than arc approximation may cause the rope to end up spawning inside a wall
        // Ideally this should be solved by making multiple arcs, but im too tired for this shit
        arcApproximation = arcApproximation && distance > 0 && distance / rope.Comp.RopeLength > 0.5;

        if (arcApproximation)
        {
            using var arcPointsEnum = DistributePointsOnArc(leftPos, rightPos, rope.Comp.RopeLength, rope.Comp.LinkCount).GetEnumerator();
            using var linksEnum = rope.Comp.Links.GetEnumerator();

            while (linksEnum.MoveNext() && arcPointsEnum.MoveNext())
            {
                var link = linksEnum.Current;
                var point = arcPointsEnum.Current;

                var xform = Transform(link.LinkEntity);
                _xform.SetMapCoordinates((link.LinkEntity, xform), new(point, map));
            }
        }
        else
        {
            // This was the original implementation
            // Place each link along the line
            var segmentCount = rope.Comp.Links.Count;
            var step = distance / (segmentCount + 2);
            var normal = distance > 0 ? new(-direction.Y, direction.X) : Vector2.UnitY;

            for (var i = 0; i < segmentCount; i++)
            {
                var pos = leftPos + (i + 1) * step * direction;
                // Add a slight jitter so that if rope length >> distance, it doesn't just end pushing the entities apart
                // If we just stack the joints in a line, we'll create a giant spring, which is bad
                pos += normal * 0.1f * (i % 2);

                var link = rope.Comp.Links[i];
                var xform = Transform(link.LinkEntity);
                _xform.SetMapCoordinates((link.LinkEntity, xform), new(pos, map));
            }
        }
    }

    // Common behavior for ConnectStart and ConnectEnd when the rope has no links
    private void ConnectRopeWithNoJoints(Entity<RopeComponent> rope,
        EntityUid leftAnchor,
        EntityUid rightAnchor,
        Vector2 offsetLeft,
        Vector2 offsetRight)
    {
        var joint = CreateDistanceJoint(leftAnchor, rightAnchor, rope.Comp, offsetLeft, offsetRight);

        rope.Comp.ConnectedStart = new(leftAnchor, joint.ID, offsetLeft);
        rope.Comp.ConnectedEnd = new(rightAnchor, joint.ID, offsetRight);

        OnRopeAttached(rope!, leftAnchor);
        OnRopeAttached(rope!, rightAnchor);
    }

    /// <summary>
    ///     Sets the position of all the links of the rope to the given position.
    ///     Entities will end up stacked.
    ///     Does not teleport the attached entities.
    /// </summary>
    public void SetLinksCoordinates(Entity<RopeComponent?> rope, EntityCoordinates refCoords)
    {
        if (!Resolve(rope, ref rope.Comp) || rope.Comp.IsDisabled || _net.IsClient)
            return;

        var coords = _xform.ToMapCoordinates(refCoords);
        foreach (var link in rope.Comp.Links)
        {
            // Add small jitter so that we don't stack all the links on the same spot (this would cause infinite tension and would break the rope)
            var jitter = _random.NextVector2(0.3f);
            _xform.SetMapCoordinates(link.LinkEntity, coords.Offset(jitter));
        }
    }

    /// <summary>
    ///     Sets the length of the rope. Can lead to non-physical behavior.
    /// </summary>
    public void SetRopeLength(Entity<RopeComponent?> rope, float length)
    {
        if (!Resolve(rope, ref rope.Comp) || _net.IsClient)
            return;

        var linkCount = rope.Comp.Links.Count;
        var linkLength = linkCount > 0 ? length / linkCount : length;

        rope.Comp.RopeLength = length;
        rope.Comp.LinkLength = linkLength;

        foreach (var joint in EnumerateRopeJoints(rope!))
            SetLinkLength(joint, linkLength);

        // This is very fucky-wucky. The client doesn't receive joint state updates when we modify fields, so we need to force-feed them to it.
        DirtyAllLinkJoints(rope!);
    }

    public void SetRopeColor(Entity<RopeComponent?> rope, Color? color)
    {
        if (!Resolve(rope, ref rope.Comp))
            return;

        rope.Comp.Color = color;
        Dirty(rope);
    }

    /// <summary>
    ///     Sets the number of links of the rope. Will partially re-create the rope.
    ///     Prototype and spawn coords are determined automatically if not specified.
    /// </summary>
    /// <summary>If this method is called AFTER the rope is enabled, the caller needs to either mark the rope for re-creation or distribute links manually.</summary>
    public void SetRopeLinks(Entity<RopeComponent?> rope, int linkCount, RopeConfigurationPrototype? prototype = null, MapCoordinates? spawnCoords = null)
    {
        if (!Resolve(rope, ref rope.Comp) || _net.IsClient)
            return;

        if (prototype == null && !_protoMan.Resolve(rope.Comp.Configuration, out prototype))
            return;

        spawnCoords ??= _xform.GetMapCoordinates(rope);

        var length = rope.Comp.RopeLength;
        rope.Comp.LinkCount = linkCount;
        rope.Comp.LinkLength = linkCount > 0 ? length / linkCount : length;

        // Delete any previous links
        foreach (var link in rope.Comp.Links)
        {
            if (_ropeLinkQuery.TryComp(link.LinkEntity, out var linkComp))
                linkComp.Rope = EntityUid.Invalid;
            QueueDel(link.LinkEntity);
        }

        // Spawn new links
        var links = rope.Comp.Links = new();
        for (var i = 0; i < linkCount; i++)
        {
            var linkUid = Spawn(prototype.LinkPrototype, spawnCoords.Value);
            EnsureComp<RopeLinkComponent>(linkUid).Rope = rope;

            var link = new RopeComponent.Link()
            {
                LinkEntity = linkUid,
            };
            links.Add(link);
        }
    }

    /// <summary>
    ///     Queues an update on the next tick without disabling the rope.
    /// </summary>
    /// <seealso cref="RecreateRope"/>
    public void QueueUpdate(Entity<RopeComponent?> rope)
    {
        _pendingRopeUpdates.Add(rope);
    }

    /// <summary>
    ///     Enables or disables the rope based on whether it can or can not exist.
    /// </summary>
    public void UpdateRope(Entity<RopeComponent> rope)
    {
        if (_net.IsClient)
            return;

        var has1Anchor = rope.Comp.ConnectedStart != null || rope.Comp.ConnectedEnd != null;
        var has2Anchors = rope.Comp.ConnectedStart != null && rope.Comp.ConnectedEnd != null;

        // We can enable the rope in one of the two following scenarios:
        // 1. It's attached to exactly 1 anchor
        // 2. It's attached to two anchors, and a rope can exist between them
        var disabled = rope.Comp.IsDisabled;
        var shouldEnable = (!has2Anchors && has1Anchor)
            || (has2Anchors && !ShouldTemporarilyDisableRope(rope.Comp.ConnectedStart!.Value.Anchor, rope.Comp.ConnectedEnd!.Value.Anchor));

        if (disabled && shouldEnable)
        {
            // If we can't enable it, it's invalid
            if (!EnableRope(rope!))
            {
                Log.Warning($"Rope {ToPrettyString(rope)} cannot be re-enabled. Deleting it.");
                TryQueueDel(rope);
            }
        }
        else if (!disabled && !shouldEnable)
            DisableRope(rope!);
        else if (disabled && !shouldEnable)
        {
            // This is a workaround - since the rope is only attached to anchors in EnableRope,
            // if the rope gets created in a temporarily-disabled state (e.g. when attaching a leash to yourself),
            // its anchors won't receive a RopeAttachedComponent and we won't be able to track when the rope leaves the temporarily-disabled state
            if (rope.Comp.ConnectedStart is {} start)
                OnRopeAttached(rope!, start.Anchor);
            if (rope.Comp.ConnectedEnd is {} end)
                OnRopeAttached(rope!, end.Anchor);
        }
    }

    /// <summary>
    ///     Sends all links of the rope to nullspace and disables all relevant joints.
    /// </summary>
    public void DisableRope(Entity<RopeComponent?> rope)
    {
        if (!_ropeQuery.Resolve(rope, ref rope.Comp) || rope.Comp.IsDisabled)
            return;

        Log.Debug($"Disabling rope {rope}");
        rope.Comp.IsDisabled = true;

        // Delete joints. Creating a list first to avoid issues.
        foreach (var joint in EnumerateRopeJoints(rope!).ToList())
            _joints.RemoveJoint(joint);

        // Detach links to nullspace
        foreach (var link in rope.Comp.Links)
        {
            _xform.DetachEntity(link.LinkEntity);
            link.LeftJoint = _invalidJointMarker;
            link.RightJoint = _invalidJointMarker;
        }

        // Set invalid joint ids
        if (rope.Comp.ConnectedStart is { } start)
            rope.Comp.ConnectedStart = start with { JointId = _invalidJointMarker };

        if (rope.Comp.ConnectedEnd is { } end)
            rope.Comp.ConnectedEnd = end with { JointId = _invalidJointMarker };

        RaiseLocalEvent(rope, new RopeDisabledEvent());
    }

    /// <summary>
    ///     Enables a previously disabled rope and places all of its links either between the two anchors or near the left or right anchor (whichever exists).
    /// </summary>
    /// <remarks>Does not check if the anchors are on the same map.</remarks>
    public bool EnableRope(Entity<RopeComponent?> rope, bool skipChecks = false)
    {
        if (!_ropeQuery.Resolve(rope, ref rope.Comp) || !rope.Comp.IsDisabled)
            return false;

        Log.Debug($"Enabling rope {rope}");

        // Move links
        var leftAnchor = rope.Comp.ConnectedStart;
        var rightAnchor = rope.Comp.ConnectedEnd;

        if (leftAnchor != null && rightAnchor != null)
        {
            // If there are two anchors, we need to make sure their positions are valid
            if (!skipChecks && !CanRopeExistBetween(leftAnchor.Value.Anchor, rightAnchor.Value.Anchor, rope.Comp.RopeLength))
            {
                Log.Warning($"Rope {ToPrettyString(rope)} has two anchors but they are too far away.");
                return false;
            }

            DistributeLinksBetween(leftAnchor.Value.Anchor, rightAnchor.Value.Anchor, rope!);
        }
        else if (leftAnchor != null)
            SetLinksCoordinates(rope, Transform(leftAnchor.Value.Anchor).Coordinates);
        else if (rightAnchor != null)
            SetLinksCoordinates(rope, Transform(rightAnchor.Value.Anchor).Coordinates);
        else
        {
            Log.Warning($"Rope {ToPrettyString(rope)} has neither a left nor a right connector. Cannot re-enable it.");
            return false;
        }

        // Create joints between consecutive links
        var segmentCount = rope.Comp.Links.Count;
        for (var i = 1; i < segmentCount; i++)
        {
            var a = rope.Comp.Links[i - 1];
            var b = rope.Comp.Links[i];
            var joint = CreateDistanceJoint(a.LinkEntity, b.LinkEntity, rope.Comp);
            a.RightJoint = b.LeftJoint = joint.ID;
        }

        // Must be done before TryConnect is called, otherwise they will skip creating joints
        rope.Comp.IsDisabled = false;

        // Connect start and end
        if (segmentCount > 0)
        {
            if (leftAnchor != null)
                TryConnectRopeStart(rope, leftAnchor.Value.Anchor, leftAnchor.Value.Offset);
            if (rightAnchor != null)
                TryConnectRopeEnd(rope, rightAnchor.Value.Anchor, rightAnchor.Value.Offset);
        }
        else
        {
            if (leftAnchor != null && rightAnchor != null)
                ConnectRopeWithNoJoints(rope!, leftAnchor.Value.Anchor, rightAnchor.Value.Anchor, rightAnchor.Value.Offset, rightAnchor.Value.Offset);
        }

        RaiseLocalEvent(rope, new RopeEnabledEvent());

        return true;
    }

    public bool IsDisabled(Entity<RopeComponent?> rope)
    {
        if (!_ropeQuery.Resolve(rope, ref rope.Comp, logMissing: false))
            return true; // We return true here because it likely means the rope was deleted (happens in OnJointRemoved)

        return rope.Comp.IsDisabled;
    }

    /// <summary>
    ///     Creates a rope entity and all of its links at the given coordinates (stacking them in the same spot).
    ///     EnableRope needs to be called in order to actually create joints.
    /// </summary>
    public Entity<RopeComponent> CreateRopeEntityUninitialized(RopeConfigurationPrototype config, float length, MapCoordinates coords)
    {
        var ropeUid = Spawn(config.DataPrototype, coords);
        var rope = EnsureComp<RopeComponent>(ropeUid);
        var linkCount = config.Links;

        rope.Configuration = config;
        rope.RopeLength = length;
        rope.LinkStiffness = config.Stiffness;
        rope.IsDisabled = true;

        // Spawn links
        SetRopeLinks((ropeUid, rope), linkCount, config, coords);

        return (ropeUid, rope);
    }
}
