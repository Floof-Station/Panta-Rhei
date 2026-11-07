using Content.Server._CD.Body.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared._CD.Body;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Content.Shared.Metabolism;
using Robust.Shared.Timing;

namespace Content.Server._CD.Body.Systems;

public sealed class AllergySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<AllergyComponent, ReactionEntityEvent>(OnReaction);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var allergies = args.Profile.CDAllergies;
        if (allergies is { Count: > 0 })
            AddComp(args.Mob, new AllergyComponent { Reagents = allergies });
    }

    private void OnReaction(EntityUid uid, AllergyComponent allergy, ref ReactionEntityEvent args)
    {
        if (!allergy.Reagents.TryGetValue(args.Reagent.ID, out var allergyData))
            return;
        if (!TryComp(uid, out BloodstreamComponent? bloodstream))
            return;
        if (!_solutionContainerSystem.ResolveSolution(uid,
                bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution,
                out var solution))
            return;
        var quantity = args.ReagentQuantity.Quantity;
        solution.AddReagent(allergyData.ReactionReagent, allergyData.Intensity * quantity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AllergyComponent, MetabolizerComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var allergy, out var metabolizer, out var bloodstream))
        {
            if (_gameTiming.CurTime < allergy.NextUpdate)
                continue;

            allergy.NextUpdate += metabolizer.UpdateInterval;

            if (allergy.Reagents.Count == 0)
                continue;

            if (!_solutionContainerSystem.ResolveSolution(uid,
                    bloodstream.BloodSolutionName,
                    ref bloodstream.BloodSolution,
                    out var chemstream))
                continue;

            // Get reactions from the chemstream
            ApplyReaction(allergy, chemstream, chemstream);

            // Add reactions from organs (currently, only lungs)
            if (TryComp<BodyComponent>(uid, out var body) && body.Organs is {} organs)
            {
                foreach (var organ in organs.ContainedEntities)
                {
                    if (TryComp<LungComponent>(organ, out var lung) && lung.Solution != null)
                        ApplyReaction(allergy, lung.Solution!.Value.Comp.Solution, chemstream);
                }
            }
        }
    }

    private void ApplyReaction(AllergyComponent allergy, Solution source, Solution destination)
    {
        foreach (var reagent in source.Contents)
        {
            if (!allergy.Reagents.TryGetValue(reagent.Reagent.Prototype, out var allergyData))
            continue;

            destination.AddReagent(allergyData.ReactionReagent, allergyData.Intensity * reagent.Quantity);
        }
    }
}
