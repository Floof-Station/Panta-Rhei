using Content.Shared._DV.Traits.Effects;
using Content.Shared._DV.Traits;
using Content.Shared._Coyote.SniffAndSmell;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
/// Effect that adds a scent trait,
/// </summary>
public sealed partial class AddScentTraitEffect : BaseTraitEffect
{
    [DataField(required: true)] public List<ProtoId<ScentPrototype>> Scents;

    public override void Apply(TraitEffectContext ctx)
    {
        var scentSystem = ctx.EntMan.System<ScentSystem>();

        var sortedPrototypes = new List<TraitPrototype>();
        var scentComp = ctx.EntMan.EnsureComponent<ScentComponent>(ctx.Player);

        foreach (var scent in Scents)
        {
            scentSystem.AddScentPrototype(scentComp, scent);
        }
    }
}
