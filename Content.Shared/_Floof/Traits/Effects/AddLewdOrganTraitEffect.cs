using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.Lewd.Components;
using Content.Shared._Floof.Lewd.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class AddLewdOrganEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public EntProtoId<LewdOrganComponent> Organ;

    public override void Apply(TraitEffectContext ctx)
    {
        var lewdSys = ctx.EntMan.System<LewdOrganSystem>();
        try
        {
            // Guaranteed to have a LewdOrgan as per above
            var organ = ctx.EntMan.Spawn(Organ, doMapInit: true);
            var organComp = ctx.EntMan.GetComponent<LewdOrganComponent>(organ);
            lewdSys.TryAddOrganToBody((organ, organComp), ctx.Player);
        }
        catch (Exception e)
        {
            Log.Error($"Exception while trying to add organ: {e}");
        }
    }
}
