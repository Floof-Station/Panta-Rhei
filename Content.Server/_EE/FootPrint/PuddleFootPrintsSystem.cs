using System.Linq;
using Content.Shared._EE.Flight; // DeltaV
using Content.Shared._EE.FootPrint;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._EE.FootPrint;

public sealed class PuddleFootPrintsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedFlightSystem _flight = default!; // DeltaV
    [Dependency] private readonly IPrototypeManager _protoMan = default!; // Floofstation

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PuddleFootPrintsComponent, EndCollideEvent>(OnStepTrigger);
    }

    private void OnStepTrigger(EntityUid uid, PuddleFootPrintsComponent component, ref EndCollideEvent args)
    {
        if (_flight.IsFlying(uid)) // DeltaV - Flying players won't make footprints
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance)
            || !TryComp<PuddleComponent>(uid, out var puddle)
            || !TryComp<FootPrintsComponent>(args.OtherEntity, out var tripper)
            || !TryComp<SolutionContainerManagerComponent>(uid, out var solutionManager)
            || !_solutionContainer.ResolveSolution((uid, solutionManager), puddle.SolutionName, ref puddle.Solution, out var solutions))
            return;

        // Floofstation section - replaced the below
        // var totalSolutionQuantity = solutions.Contents.Sum(sol => (float) sol.Quantity);
        // var waterQuantity = (from sol in solutions.Contents where sol.Reagent.Prototype == "Water" select (float) sol.Quantity).FirstOrDefault();
        //
        // if (waterQuantity / (totalSolutionQuantity / 100f) > component.OffPercent || solutions.Contents.Count <= 0)
        //     return;
        //
        // tripper.ReagentToTransfer =
        //     solutions.Contents.Aggregate((l, r) => l.Quantity > r.Quantity ? l : r).Reagent.Prototype;
        //
        // if (_appearance.TryGetData(uid, PuddleVisuals.SolutionColor, out var color, appearance)
        //     && _appearance.TryGetData(uid, PuddleVisuals.CurrentVolume, out var volume, appearance))
        //     AddColor((Color) color, (float) volume * component.SizeRatio, tripper);
        //
        // _solutionContainer.RemoveEachReagent(puddle.Solution.Value, 0.01); //was 1

        // Transfer reagents from the puddle to the tripper.
        // Ideally it should be a two-way process, but that is too hard to simulate and will have very little effect outside of potassium-water spills.
        var quantity = puddle.Solution?.Comp?.Solution?.Volume ?? 0;
        var footprintsCapacity = tripper.ContainedSolution.AvailableVolume;

        if (quantity <= 0 || footprintsCapacity <= 0)
            return;

        var transferAmount = FixedPoint2.Min(footprintsCapacity, quantity * component.SizeRatio);
        var transferred = _solutionContainer.SplitSolution(puddle.Solution!.Value, transferAmount);
        tripper.ContainedSolution.AddSolution(transferred, _protoMan);
        // Floofstation section end
    }

    // Floofstation - removed
    // private void AddColor(Color col, float quantity, FootPrintsComponent component)
    // {
    //     component.PrintsColor = component.ColorQuantity == 0f ? col : Color.InterpolateBetween(component.PrintsColor, col, component.ColorInterpolationFactor);
    //     component.ColorQuantity += quantity;
    // }
}
