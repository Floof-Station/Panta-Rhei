using Content.Shared._DV.Traits.Effects;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.ComponentModel;
using System.Xml.Linq;

namespace Content.Shared._Euphoria.Traits.Effects;

public sealed partial class ReplaceBloodReagentTraitEffect : BaseTraitEffect
{

    [DataField(required: true)]
    public string Reagent = null;

    public override void Apply(TraitEffectContext ctx) {
        if (ctx.EntMan.TryGetComponent<BloodstreamComponent>(ctx.Player, out var bloodstream))
        {
            var referenceSolution = bloodstream.BloodReferenceSolution;
            var totalVolume = referenceSolution.Volume;
            referenceSolution.RemoveAllSolution();
            referenceSolution.AddReagent(Reagent, totalVolume);
            if (
                ctx.EntMan.TryGetComponent<ContainerManagerComponent>(ctx.Player, out var containerManager) &&
                containerManager.Containers.TryGetValue($"solution@{bloodstream.BloodSolutionName}", out var solutionContainer) &&
                solutionContainer is ContainerSlot solutionSlot &&
                solutionSlot.ContainedEntity is { } containedSolution
            )
            {
                var bloodSolution = ctx.EntMan.GetComponent<SolutionComponent>(containedSolution).Solution;
                bloodSolution.RemoveAllSolution();
                bloodSolution.AddReagent(Reagent, totalVolume);
            }
        }
    }
}
