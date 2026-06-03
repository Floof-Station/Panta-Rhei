using Content.Shared._Floof.InteractionVerbs;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Server._Floof.InteractionVerbs.Actions;

[Serializable]
public sealed partial class ChangeStandingStateAction : InteractionAction
{
    [DataField]
    public bool MakeStanding, MakeLaying;

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        if (!deps.EntMan.TryGetComponent<StandingStateComponent>(args.Target, out var state))
            return false;

        if (isBefore)
            args.Blackboard["standing"] = state.Standing;

        return state.Standing == true && MakeLaying
               || state.Standing == false && MakeStanding;
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        // For some reason both StandingState and SharedStunSystem are responsible for standing
        // real shitcode
        var stateSystem = deps.EntMan.System<StandingStateSystem>();
        var stunSystem = deps.EntMan.System<SharedStunSystem>();
        if (!deps.EntMan.TryGetComponent<StandingStateComponent>(args.Target, out var state)
            || args.TryGetBlackboard("standing", out bool oldStanding) && oldStanding != state.Standing)
            return false;

        // Note: these will get cancelled if the target is forced to stand/lay, e.g. due to a buckle or stun or something else.
        var isStanding = state.Standing;
        if (!isStanding && MakeStanding)
        {
            // TryStand requires a KnockedDownComponent. Fucked up. Also, it won't even make them stand up - we have to set AutoStand.
            if (deps.EntMan.TryGetComponent<KnockedDownComponent>(args.Target, out var knockedDown))
            {
                stunSystem.TryStand((args.Target, knockedDown));
                stunSystem.SetAutoStand((args.Target, knockedDown), true);
                return true;
            }
            return stateSystem.Stand(args.Target);
        }
        else if (isStanding && MakeLaying)
        {
            if (stunSystem.TryCrawling(args.Target, null, autoStand: false, drop: true))
                return true;
            return stateSystem.Down(args.Target);
        }

        return false;
    }
}
