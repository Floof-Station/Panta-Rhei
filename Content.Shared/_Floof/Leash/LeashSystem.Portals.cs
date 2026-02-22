using Content.Shared.Teleportation.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    private void StartThinkingWithPortals()
    {
        SubscribeLocalEvent<TeleportedEvent>(OnTeleported);
    }

    private void OnTeleported(TeleportedEvent ev)
    {
        // The client assumes infinite joint length, so it doesn't matter there
        if (!ShouldPredictLeashes())
            return;

        if (TryComp<JointComponent>(ev.Subject, out var joints))
            BreakLeashJoints(joints);

        if (TryComp<JointRelayTargetComponent>(ev.Subject, out var relay))
        {
            foreach (var entity in relay.Relayed)
            {
                if (TryComp<JointComponent>(entity, out var jointsRelayed))
                    BreakLeashJoints(jointsRelayed);
            }
        }
        return;

        // Break all leash joints on the entity
        // This will raise JointRemovedEvent and queue a joint refresh on the next tick
        // By this time the entity will have finished teleporting so we can know if the leash can be preserved or not
        void BreakLeashJoints(JointComponent joints)
        {
            foreach (var (id, joint) in joints.GetJoints)
            {
                if (!id.StartsWith(LeashJointIdPrefix))
                    continue;

                _joints.RemoveJoint(joint);
            }
        }
    }
}
