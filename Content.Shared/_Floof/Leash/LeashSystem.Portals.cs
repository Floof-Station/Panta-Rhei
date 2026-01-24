using Content.Shared.Teleportation.Systems;
using Robust.Shared.Physics;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    private void StartThinkingWithPortals()
    {
        SubscribeLocalEvent<TeleportedEvent>(OnTeleported);
    }

    private void OnTeleported(TeleportedEvent ev)
    {
        // Break all leash joints on the entity
        // This will raise JointRemovedEvent and queue a joint refresh on the next frame
        // By this time the entity will have finished teleporting so we can know if the leash can be preserved or not
        if (!TryComp<JointComponent>(ev.Subject, out var joints))
            return;

        // The client assumes infinite joint length, so it doesn't matter there
        if (!ShouldPredictLeashes())
            return;

        foreach (var (id, joint) in joints.GetJoints)
        {
            if (!id.StartsWith(LeashJointIdPrefix))
                continue;

            _joints.RemoveJoint(joint);
        }
    }
}
