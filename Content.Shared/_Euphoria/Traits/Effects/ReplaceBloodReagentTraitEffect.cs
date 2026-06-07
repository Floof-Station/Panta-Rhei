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

    [DataField("reagent", required: true)]
    public string Reagent;

    public override void Apply(TraitEffectContext ctx) {
        if (ctx.EntMan.TryGetComponent<BloodstreamComponent>(ctx.Player, out var bloodstream))
        {
            var referenceSolution = bloodstream.BloodReferenceSolution;
            var totalVolume = referenceSolution.Volume;
            referenceSolution.RemoveAllSolution();
            referenceSolution.AddReagent(Reagent, totalVolume);
            if (bloodstream.BloodSolution.HasValue)
            {
                var bloodSolution = ctx.EntMan.GetComponent<SolutionComponent>(bloodstream.BloodSolution.Value).Solution;
                bloodSolution.RemoveAllSolution();
                bloodSolution.AddReagent(Reagent, totalVolume);
            }
        }
    }
}
