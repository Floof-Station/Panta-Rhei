using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Shared._Floof.InteractionVerbs;
using Content.Shared._Floof.Lewd;
using Content.Shared._Floof.Lewd.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server._Floof.InteractionVerbs.Actions.Lewd;

public sealed partial class LewdFillContainerFromTarget : BaseLewdOrganAction
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
        // Code duplication buut idc man
        var handsSys = deps.System<HandsSystem>();
        var solSystem = deps.System<SharedSolutionContainerSystem>();
        if (!handsSys.TryGetActiveItem(args.User, out var container)
            || !solSystem.TryGetRefillableSolution(container.Value, out var targetSolEnt, out _))
            return false;

        var lewdSys = deps.System<LewdOrganSystem>();
        if (!lewdSys.TryGetOrganSolution(Organ, args.Target, out var solution, out var solEnt))
            return false;

        return true;
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var handsSys = deps.System<HandsSystem>();
        var solSystem = deps.System<SharedSolutionContainerSystem>();
        if (!handsSys.TryGetActiveItem(args.User, out var container)
            || !solSystem.TryGetRefillableSolution(container.Value, out var targetSolEnt, out var targetSol))
            return false;

        var lewdSys = deps.System<LewdOrganSystem>();
        if (!lewdSys.TryGetOrganSolution(Organ, args.Target, out _, out var sourceSolEnt))
            return false;

        var removed = solSystem.SplitSolution(sourceSolEnt.Value, MaxAmount);
        var success = removed.Volume > 0;
        solSystem.TryMixAndOverflow(targetSolEnt.Value, removed, targetSol.MaxVolume, out var overflow);

        // If the above returned false then no reagents were transferred. However, we'll still spill the rest onto the ground as players may want just that
        if (overflow is { Volume.Value: not 0 })
            deps.System<PuddleSystem>().TrySpillAt(args.Target, overflow, out _, true);

        args.AllowRepeat &= success && removed.Volume >= MinRepeatAmount; // Stop repeating if the target has way too little fluid inside
        return success; // Fail if there's nothing to draw
    }
}
