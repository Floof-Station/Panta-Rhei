using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._Floof.Ropes.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    private string _invalidJointMarker = "<TEMPORARILY DELETED>";

    private DistanceJoint CreateDistanceJoint(EntityUid a, EntityUid b, RopeComponent rope, Vector2 anchorA = default, Vector2 anchorB = default)
    {
        var id = GetEffectiveJointId(a, b);
        var joint = _joints.CreateDistanceJoint(
            a,
            b,
            anchorA,
            anchorB,
            id: id,
            minimumDistance: 0f);

        joint.MinLength = 0f; // For some fuckass reason, CreateDistanceJoint sets it to non-zero

        joint.Damping = rope.LinkStiffness * 0.1f;
        joint.Stiffness = rope.LinkStiffness;
        SetLinkLength(joint, rope.LinkLength);

        return joint;
    }

    private void SetLinkLength(DistanceJoint joint, float length)
    {
        // In case someone decides to go through a portal, we want to limit the impact, so we set it way higher than needed
        // However, if the link has no stiffness, we assume the caller wants to limit JUST the max length
        // This is a terrible hack, but I'm really fucking tired already
        var maxLengthMultiplier = joint.Stiffness <= 0.1f ? 1f : 10f;
        // Likewise. This should turn the joint off if it tries to pull from 5x its max length (such as after one of the entities teleported)
        var breakpoint = joint.Stiffness <= 0.1f ? float.PositiveInfinity : length * joint.Stiffness * 5f;

        // Note: length is how long the physics solver will try to make the joint. MaxLength is the hard limit before distances are clamped.
        joint.Length = length;
        joint.MaxLength = length * maxLengthMultiplier; // In case someone decides to go through a portal, we want to limit the impact, so we set it way higher than needed
        joint.Breakpoint = breakpoint;
    }

    private bool ResolveJoint(EntityUid anchor, string jointId, [NotNullWhen(true)] out DistanceJoint? joint)
    {
        if (!TryComp<JointComponent>(anchor, out var jointComp)
            || !jointComp.GetJoints.TryGetValue(jointId, out var jointObj)
            || jointObj is not DistanceJoint distanceJoint)
        {
            joint = null;
            return false;
        }

        joint = distanceJoint;
        return true;
    }

    private string GetEffectiveJointId(EntityUid a, EntityUid b)
    {
        var prefix = _net.IsServer ? "rj" : "rj-PREDICTED";
        return $"{prefix}-{a.Id}-{b.Id}";
    }
}
