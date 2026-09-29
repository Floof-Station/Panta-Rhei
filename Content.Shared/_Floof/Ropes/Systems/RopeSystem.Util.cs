using System.Numerics;
using Content.Shared._Floof.Ropes.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
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

    private IEnumerable<EntityUid> EnumerateRopeLinks(Entity<RopeComponent> rope)
    {
        // If there's none, we return the left anchor just so methods dirtying stuff can handle it
        if (rope.Comp.Links.Count == 0)
        {
            if (rope.Comp.ConnectedStart is { } start)
                yield return start.Anchor;
        }

        var linkCount = rope.Comp.Links.Count;
        for (var i = 0; i < linkCount; i++)
        {
            var link = rope.Comp.Links[i];
            yield return link.LinkEntity;
        }
    }

    public IEnumerable<EntityUid> EnumerateAnchors(Entity<RopeComponent> rope)
    {
        if (rope.Comp.ConnectedStart is { } start)
            yield return start.Anchor;
        if (rope.Comp.ConnectedEnd is { } end)
            yield return end.Anchor;
    }

    /// <summary>
    ///     Dirties all joint components. This is to be called after changing joint lengths or other properties WITHOUT re-creating them.
    /// </summary>
    private void DirtyAllLinkJoints(Entity<RopeComponent> rope)
    {
        var query = GetEntityQuery<JointComponent>();
        foreach (var link in EnumerateRopeLinks(rope))
        {
            if (query.TryComp(link, out var joints))
                Dirty(link, joints);
        }
    }

    // There could NOT be a worse transform API than RobustToolbox'es
    private float GetEffectiveDistance(EntityUid a, EntityUid b) => GetEffectiveDistance(Transform(a), Transform(b));

    private float GetEffectiveDistance(TransformComponent a, TransformComponent b) =>
        a.Coordinates.TryDistance(EntityManager, _xform, b.Coordinates, out var dst)
            ? dst
            : float.PositiveInfinity;

    /// <summary>
    ///     Distributes the given number of points across an arc between a and b,
    ///     trying to make it so that the arc has a length approximately equal to the desired length.
    /// </summary>
    private static IEnumerable<Vector2> DistributePointsOnArc(Vector2 a, Vector2 b, float desiredLength, int count)
    {
        var d = Math.Max(0.1f, Vector2.Distance(a, b));
        if (desiredLength < d)
            desiredLength = d;

        var normal = d > 0.01
            ? new Vector2(-(b.Y - a.Y), b.X - a.X) / d
            : new(0, 1); // That's not normal

        // Coefficient that gives the arc length approximately equal to desiredLength
        var sagitta = d > 0.01
            ? MathF.Sqrt(3f * d * (desiredLength - d) / 8f)
            : desiredLength / 2; // It's going to be a line from point A, going up and then back down

        for (var i = 0; i < count; i++)
        {
            // Basically, we take a point on the line between a and b and offset it tangentially by a "bump" function
            // The size of the bump is multiplied by a sagitta coefficient which gives the resulting arc a length that's approximately equal to the desired length
            // The math for it was mostly done by an LLM, but it seems sane enough to me
            var t = (i + 1) / (count + 2f);
            var bump = 4f * t * (1f - t); // goes from 0 (t=0) to 1 (t=0.5) to 0 (t=1)
            yield return a + (b - a) * t + normal * (sagitta * bump);
        }
    }
}
