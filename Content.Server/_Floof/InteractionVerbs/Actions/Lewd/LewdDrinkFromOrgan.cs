using Content.Server.Fluids.EntitySystems;
using Content.Shared._Floof.InteractionVerbs;
using Content.Shared._Floof.Lewd;
using Content.Shared._Floof.Lewd.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server._Floof.InteractionVerbs.Actions.Lewd;

public sealed partial class LewdDrinkFromOrgan : BaseLewdOrganAction
{
    /// <summary>
    ///     Organ on the target entity to draw from.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind Organ;

    public override bool CanPerform(InteractionArgs args,
        InteractionVerbPrototype proto,
        bool beforeDelay,
        VerbDependencies deps)
    {
        var lewdSys = deps.System<LewdOrganSystem>();
        return lewdSys.TryGetOrganSolution(Organ, args.Target, out _, out _);
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var lewdSys = deps.System<LewdOrganSystem>();
        if (!lewdSys.TryGetOrganSolution(Organ, args.Target, out var donorSol, out var donorSolEnt))
            return false;

        var solSystem = deps.System<SharedSolutionContainerSystem>();
        var removed = solSystem.SplitSolution(donorSolEnt.Value, MaxAmount);
        var success = removed.Volume > 0;

        var stomachSys = deps.System<StomachSystem>();
        if (GetBiggestStomach(deps, args.User, solSystem) is not {} stomach || !stomachSys.TryTransferSolution(stomach, removed, stomach))
        {
            // Splash if the user can't consume the liquid for some reason (no stomach?)
            // As such we don't prevent the user from trying to drink even if they cant consume it
            deps.System<PuddleSystem>().TrySpillAt(args.Target, removed, out _, true);
        }

        args.AllowRepeat &= success && removed.Volume > MinRepeatAmount; // Stop repeating if the target has way too little fluid inside
        return true;
    }

    public static Entity<StomachComponent>? GetBiggestStomach(VerbDependencies deps, EntityUid user, SharedSolutionContainerSystem solSystem)
    {
        var bodySystem = deps.System<SharedBodySystem>();
        if (!bodySystem.TryGetBodyOrganEntityComps<StomachComponent>(user, out var stomachs))
            return null;

        var highestAvailable = FixedPoint2.Zero;
        Entity<StomachComponent>? stomachToUse = null;
        foreach (var ent in stomachs)
        {
            var owner = ent.Owner;
            if (!solSystem.ResolveSolution(owner, StomachSystem.DefaultSolutionName, ref ent.Comp1.Solution, out var stomachSol))
                continue;

            if (stomachSol.AvailableVolume <= highestAvailable)
                continue;

            stomachToUse = ent;
            highestAvailable = stomachSol.AvailableVolume;
        }

        return stomachToUse;
    }
}
