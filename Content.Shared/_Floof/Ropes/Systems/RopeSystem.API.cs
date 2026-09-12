using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._Floof.Ropes.Components;
using Content.Shared._Floof.Ropes.Events;
using Content.Shared._Floof.Ropes.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    // If the distance between two entities is x, then a rope of length AT LEAST (x - tolerance) can be created between them
    private float _connectionDstTolerance = 2;
    private string _invalidJointMarker = "<TEMPORARILY DELETED>";

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
        if (rightAnchor != null && !CanRopeExistBetween(leftAnchor, rightAnchor.Value, length, leftXform))
        {
            createdRope = null;
            return false;
        }

        var rope = CreateRopeEntityUninitialized(config, length, leftXform.Coordinates);
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
        if (_xform.IsParentOf(leftXform, right)
            || _xform.IsParentOf(rightXform, left))
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

    public void DistributeLinksBetweenAnchors(Entity<RopeComponent> rope)
    {
        if (rope.Comp.ConnectedStart is not { } left || rope.Comp.ConnectedEnd is not { } right)
            return;

        DistributeLinksBetweenAnchors(left.Anchor, right.Anchor, rope);
    }

    private void DistributeLinksBetweenAnchors(EntityUid leftAnchor, EntityUid rightAnchor, Entity<RopeComponent> rope)
    {
        // Get world positions of the two anchors
        var leftXform = Transform(leftAnchor);
        var rightXform = Transform(rightAnchor);
        var map = leftXform.MapID;
        if (leftXform.MapID != rightXform.MapID)
        {
            Log.Error($"Cannot distribute leash joints between {ToPrettyString(leftAnchor)} and {ToPrettyString(rightAnchor)} as they are on different maps.");
            return;
        }

        var leftPos = _xform.GetWorldPosition(leftXform);
        var rightPos = _xform.GetWorldPosition(rightXform);
        // If leftPos == rightPos, the direction vector becomes nan
        var direction = leftPos != rightPos ? (rightPos - leftPos).Normalized() : Vector2.Zero;
        var distance = direction.Length();

        // Place each link along the line
        var segmentCount = rope.Comp.Links.Count;
        var step = distance / (segmentCount + 2);
        for (var i = 0; i < segmentCount; i++)
        {
            var pos = leftPos + (i + 1) * step * direction;
            var link = rope.Comp.Links[i];
            _xform.SetMapCoordinates(link.LinkEntity, new(pos, map));
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
    }

    // TODO code duplication?
    /// <summary>
    ///     Connects the start of the rope to the specified anchor.
    ///     If the rope has no links, this method will only have effect after both ConnectStart and ConnectEnd have been called.
    /// </summary>
    public bool TryConnectRopeStart(Entity<RopeComponent?> rope, EntityUid connector, Vector2 offset = default)
    {
        if (!Resolve(rope, ref rope.Comp) || rope.Comp.ConnectedStart is {} start && start.JointId != _invalidJointMarker)
            return false; // already attached

        if (rope.Comp.Links.Count == 0)
        {
            Log.Error("Cannot attach a rope with 0 links. Specify anchors in TryCreateRope!");
            return false;
        }

        // Check distance
        var firstLink = rope.Comp.Links[0];
        var dist = GetEffectiveDistance(connector, firstLink.LinkEntity);
        if (float.IsInfinity(dist))
            return false;

        // Create a distance joint
        var joint = CreateDistanceJoint(connector, firstLink.LinkEntity, rope.Comp, offset);
        rope.Comp.ConnectedStart = new(connector, joint.ID, offset);
        firstLink.LeftJoint = joint.ID;

        OnRopeAttached(rope, connector);

        Dirty(rope, rope.Comp);
        return true;
    }

    /// <summary>
    ///     Connects the end of the rope to the specified anchor.
    ///     If the rope has no links, this method will only have effect after both ConnectStart and ConnectEnd have been called.
    /// </summary>
    public bool TryConnectRopeEnd(Entity<RopeComponent?> rope, EntityUid connector, Vector2 offset = default)
    {
        if (!Resolve(rope, ref rope.Comp) || rope.Comp.ConnectedEnd is {} end && end.JointId != _invalidJointMarker)
            return false; // already attached

        if (rope.Comp.Links.Count == 0)
        {
            Log.Error("Cannot attach a rope with 0 links. Specify anchors in TryCreateRope!");
            return false;
        }

        // Check distance
        var lastLink = rope.Comp.Links[^1];
        var dist = GetEffectiveDistance(connector, lastLink.LinkEntity);
        if (float.IsInfinity(dist))
            return false;

        // Create a distance joint
        var joint = CreateDistanceJoint(connector, lastLink.LinkEntity, rope.Comp, Vector2.Zero, offset);
        rope.Comp.ConnectedEnd = new(connector, joint.ID, offset);
        lastLink.RightJoint = joint.ID;

        OnRopeAttached(rope, connector);

        Dirty(rope, rope.Comp);
        return true;
    }

    public bool TryDetachStart(Entity<RopeComponent?> rope)
    {
        if (!Resolve(rope, ref rope.Comp) || rope.Comp.ConnectedStart == null)
            return false;

        if (rope.Comp.Links.Count == 0)
        {
            Log.Error("Cannot detach a rope with 0 links. Delete the rope entity instead!");
            return false;
        }

        var firstLink = rope.Comp.Links[0];
        _joints.RemoveJoint(firstLink.LinkEntity, rope.Comp.ConnectedStart.Value.JointId);

        rope.Comp.ConnectedStart = null;
        firstLink.LeftJoint = null;

        OnRopeDetached(rope, firstLink.LinkEntity);

        Dirty(rope, rope.Comp);
        return true;
    }

    public bool TryDetachEnd(Entity<RopeComponent?> rope)
    {
        if (!Resolve(rope, ref rope.Comp) || rope.Comp.ConnectedEnd == null)
            return false;

        if (rope.Comp.Links.Count == 0)
        {
            Log.Error("Cannot detach a rope with 0 links. Delete the rope entity instead!");
            return false;
        }

        var lastLink = rope.Comp.Links[^1];
        _joints.RemoveJoint(lastLink.LinkEntity, rope.Comp.ConnectedEnd.Value.JointId);

        rope.Comp.ConnectedEnd = null;
        lastLink.RightJoint = null;

        OnRopeDetached(rope, lastLink.LinkEntity);

        Dirty(rope, rope.Comp);
        return true;
    }

    private void OnRopeAttached(Entity<RopeComponent?> rope, EntityUid connector)
    {
        if (!_ropeAttachedQuery.TryComp(connector, out var ropeAttachedComp))
            ropeAttachedComp = AddComp<RopeAttachedComponent>(connector);

        var args = new RopeAttachedComponent.AttachedRopeInfo(rope, null, null);
        ropeAttachedComp.AttachedRopes.Add(args);

        ProcessRelay(connector, args);
    }

    private void OnRopeDetached(Entity<RopeComponent?> rope, EntityUid connector)
    {
        if (!_ropeAttachedQuery.TryComp(connector, out var ropeAttachedComp)
            || (ropeAttachedComp.IndexOfRope(rope) is var index && index != -1))
            return;

        var relayInfo = ropeAttachedComp.AttachedRopes[index];
        ropeAttachedComp.AttachedRopes.RemoveAt(index);

        RemoveRelay(connector, relayInfo);
    }

    /// <summary>
    ///     Sets the position of all the links of the rope to the given position.
    ///     Entities will end up stacked.
    ///     Does not teleport the attached entities.
    /// </summary>
    public void SetLinksCoordinates(Entity<RopeComponent?> rope, EntityCoordinates coords)
    {
        if (!Resolve(rope, ref rope.Comp) || rope.Comp.IsDisabled)
            return;

        foreach (var link in rope.Comp.Links)
            _xform.SetCoordinates(link.LinkEntity, coords);
    }

    /// <summary>
    ///     Sets the length of the rope. Can lead to non-physical behavior.
    /// </summary>
    public void SetRopeLength(Entity<RopeComponent?> rope, float length)
    {
        if (!Resolve(rope, ref rope.Comp))
            return;

        var linkCount = rope.Comp.Links.Count;
        var linkLength = linkCount > 0 ? length / linkCount : length;

        rope.Comp.RopeLength = length;
        rope.Comp.LinkLength = linkLength;

        foreach (var joint in EnumerateRopeJoints(rope!))
        {
            SetLinkLength(joint, linkLength);
        }
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

            DistributeLinksBetweenAnchors(leftAnchor.Value.Anchor, rightAnchor.Value.Anchor, rope!);
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

        rope.Comp.IsDisabled = false;

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
    public Entity<RopeComponent> CreateRopeEntityUninitialized(RopeConfigurationPrototype config, float length, EntityCoordinates coords)
    {
        var ropeUid = Spawn(config.DataPrototype, coords);
        var rope = EnsureComp<RopeComponent>(ropeUid);
        var segmentCount = config.Links;

        rope.Configuration = config;
        rope.RopeLength = length;
        rope.LinkLength = segmentCount == 0 ? length : length / config.Links;
        rope.LinkStiffness = config.Stiffness;
        rope.IsDisabled = true;

        // Spawn links
        var links = rope.Links = new();
        for (var i = 0; i < segmentCount; i++)
        {
            var linkUid = Spawn(config.LinkPrototype, coords);
            EnsureComp<RopeLinkComponent>(linkUid).Rope = ropeUid;

            var link = new RopeComponent.Link()
            {
                LinkEntity = linkUid,
            };
            links.Add(link);
        }

        return (ropeUid, rope);
    }

    private IEnumerable<DistanceJoint> EnumerateRopeJoints(Entity<RopeComponent> rope)
    {
        if (rope.Comp.IsDisabled)
            yield break;

        if (rope.Comp.ConnectedStart is { } start && ResolveJoint(start.Anchor, start.JointId, out var startJoint))
            yield return startJoint;

        // If this is a linkless rope, the joint we just fetched above is the only joint (the yield return below points to the same joint)
        if (rope.Comp.Links.Count == 0)
            yield break;

        if (rope.Comp.ConnectedEnd is { } end && ResolveJoint(end.Anchor, end.JointId, out var endJoint))
            yield return endJoint;

        // Links also store joints connecting them on the left and right.
        // We skip the last one cause it's the same as one found in the above ConnectedEnd clause
        var linkCount = rope.Comp.Links.Count;
        for (var i = 0; i < linkCount - 1; i++)
        {
            var link = rope.Comp.Links[i];

            // RightJoint should never be null on any link other than the last
            DebugTools.Assert(link.RightJoint != null);

            if (ResolveJoint(link.LinkEntity, link.RightJoint!, out var joint))
                yield return joint;
        }
    }

    // There could NOT be a worse transform API than RobustToolbox'es
    private float GetEffectiveDistance(EntityUid a, EntityUid b) => GetEffectiveDistance(Transform(a), Transform(b));

    private float GetEffectiveDistance(TransformComponent a, TransformComponent b) =>
        a.Coordinates.TryDistance(EntityManager, _xform, b.Coordinates, out var dst)
            ? dst
            : float.PositiveInfinity;
}
