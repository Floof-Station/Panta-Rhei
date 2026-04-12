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

public sealed partial class LewdOrganFluidTransfer : InteractionAction
{
    /// <summary>
    ///     Organ on the user entity to draw from.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind DonorOrgan;

    /// <summary>
    ///     Organ on the target entity to deposit into.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind ReceiverOrgan;

    [DataField]
    public FixedPoint2 MaxAmount = FixedPoint2.MaxValue;

    public override bool IsAllowed(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps) => CanPerform(args, proto, true, deps);

    public override bool CanPerform(InteractionArgs args,
        InteractionVerbPrototype proto,
        bool beforeDelay,
        VerbDependencies deps)
    {
        var lewdSys = deps.System<LewdOrganSystem>();
        if (!lewdSys.TryGetOrganSolution(DonorOrgan, args.User, out _, out _))
            return false;

        if (!lewdSys.TryGetOrganSolution(ReceiverOrgan, args.Target, out _, out _))
            return false;

        return true;
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var lewdSys = deps.System<LewdOrganSystem>();
        if (!lewdSys.TryGetOrganSolution(DonorOrgan, args.User, out _, out var donorSolEnt))
            return false;

        if (!lewdSys.TryGetOrganSolution(ReceiverOrgan, args.Target, out var receiverSol, out var receiverSolEnt))
            return false;

        var solSystem = deps.System<SharedSolutionContainerSystem>();
        var removed = solSystem.SplitSolution(donorSolEnt.Value, MaxAmount);
        solSystem.TryMixAndOverflow(receiverSolEnt.Value, removed, receiverSol.MaxVolume, out var overflow);

        // Splash.
        if (overflow is { Volume.Value: not 0 })
            deps.System<PuddleSystem>().TrySpillAt(args.Target, overflow, out _, true);

        return true;
    }
}
