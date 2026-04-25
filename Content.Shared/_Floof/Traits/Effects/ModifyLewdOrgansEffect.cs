using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.Lewd;
using Content.Shared._Floof.Lewd.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class ModifyLewdOrgansEffect : BaseTraitEffect
{
    /// <summary>
    ///     Which organ flags to target.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind Target;

    [DataField]
    public float ProductionMultiplier = 1;

    [DataField]
    public float ProductionCapMultiplier = 1;

    [DataField]
    public FixedPoint2 DepositionCapacityBonus = 0;

    [DataField]
    public bool? SpillDrain = null;

    public override void Apply(TraitEffectContext ctx)
    {
        var lewdSys = ctx.EntMan.System<LewdOrganSystem>();
        foreach (var (organUid, organ, lewdOrgan) in lewdSys.GetLewdOrgans(ctx.Player))
        {
            var data = lewdOrgan.Data;
            if ((data.OrganKind & Target) == 0)
                continue;

            // Uuuuugh, I hate using mutable semantics here, but RT's support for immutables sucks ass (and i don't want to make a 300 line class to support them)
            data.ProductionSpeed *= ProductionMultiplier;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (ProductionCapMultiplier != 1 && data.ProducedReagents is not null)
                for (var i = 0; i < data.ProducedReagents.Length; i++)
                    data.ProducedReagents[i] = new(data.ProducedReagents[i].Reagent, data.ProducedReagents[i].Quantity * ProductionCapMultiplier);

            if (DepositionCapacityBonus != 0)
                data.SolutionVolume += DepositionCapacityBonus;

            if (SpillDrain is not null)
                data.SpillDrain = SpillDrain.Value;

            lewdSys.UpdateData(data);
            lewdSys.UpdateOrgan((organUid, lewdOrgan), ctx.Player);
        }
    }
}
