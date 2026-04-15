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

public sealed partial class LewdOrganFluidTransfer : BaseLewdOrganAction
{
    /// <summary>
    ///     Organ on the user entity to draw from.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind DonorOrgan;

    /// <summary>
    ///     Organ on the target entity to deposit into. If set to null, spills all the liquid instead.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind? ReceiverOrgan;

    public override bool CanPerform(InteractionArgs args,
        InteractionVerbPrototype proto,
        bool beforeDelay,
        VerbDependencies deps)
    {
        var lewdSys = deps.System<LewdOrganSystem>();
        if (!lewdSys.TryGetOrganSolution(DonorOrgan, args.User, out _, out _))
            return false;

        if (ReceiverOrgan != null && !lewdSys.TryGetOrganSolution(ReceiverOrgan.Value, args.Target, out _, out _))
            return false;

        return true;
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var lewdSys = deps.System<LewdOrganSystem>();
        if (!lewdSys.TryGetOrganSolution(DonorOrgan, args.User, out _, out var donorSolEnt))
            return false;

        var solSystem = deps.System<SharedSolutionContainerSystem>();
        var removed = solSystem.SplitSolution(donorSolEnt.Value, MaxAmount);
        var success = removed.Volume > 0;

        Solution? overflow;
        if (ReceiverOrgan != null && lewdSys.TryGetOrganSolution(ReceiverOrgan.Value, args.Target, out var receiverSol, out var receiverSolEnt))
            solSystem.TryMixAndOverflow(receiverSolEnt.Value, removed, receiverSol.MaxVolume, out overflow);
        else
            overflow = removed; // Splash everything if couldn't transfer

        // Splash.
        if (overflow is { Volume.Value: not 0 })
            deps.System<PuddleSystem>().TrySpillAt(args.Target, overflow, out _, true);

        args.AllowRepeat &= success && removed.Volume > MinRepeatAmount; // Stop repeating if the target has way too little fluid inside
        return true;
    }
}
