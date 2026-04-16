using Content.Shared._EE.Flight;
using Content.Shared._EE.FootPrint;
using Content.Shared._Floof.Footprint;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
// DeltaV

namespace Content.Server._Floof.FootPrint;

public sealed class PuddleFootPrintsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedFlightSystem _flight = default!; // DeltaV
    [Dependency] private readonly IPrototypeManager _protoMan = default!; // Floofstation
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PuddleFootPrintsComponent, EndCollideEvent>(OnStepTrigger);
    }

    private void OnStepTrigger(EntityUid uid, PuddleFootPrintsComponent component, ref EndCollideEvent args)
    {
        // Floofstation note: the below checks ensure OtherBody is a puddle nad uid is not
        // This check was expanded to accomodate for non-flight-related flying
        if (_flight.IsFlying(uid) || TryComp<PhysicsComponent>(uid, out var phys) && phys.BodyStatus == BodyStatus.InAir)
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance)
            || !TryComp<PuddleComponent>(uid, out var puddle)
            || !TryComp<FootPrintsComponent>(args.OtherEntity, out var tripper)
            || !TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager)
            || !_solutionContainer.ResolveSolution((uid, solutionManager), puddle.SolutionName, ref puddle.Solution, out var solutions))
            return;

        // Transfer reagents from the puddle to the tripper.
        // Ideally it should be a two-way process, but that is too hard to simulate and will have very little effect outside of potassium-water spills.
        var quantity = puddle.Solution?.Comp?.Solution?.Volume ?? 0;
        var footprintsCapacity = tripper.ContainedSolution.AvailableVolume;
        var transferAmount = FixedPoint2.Min(footprintsCapacity, quantity * component.SizeRatio);
        if (quantity <= 0 || footprintsCapacity <= 0 || transferAmount <= 0)
            return;

        var transferred = _solutionContainer.SplitSolution(puddle.Solution!.Value, transferAmount);
        tripper.ContainedSolution.AddSolution(transferred, _protoMan);
    }
}
