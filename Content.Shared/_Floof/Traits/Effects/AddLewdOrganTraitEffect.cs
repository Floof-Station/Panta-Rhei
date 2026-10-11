using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.Lewd.Components;
using Content.Shared._Floof.Lewd.Systems;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class AddLewdOrganEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public EntProtoId<LewdOrganComponent> Organ;

    public override void Apply(TraitEffectContext ctx)
    {
        var player = ctx.Player;
        if (!ctx.EntMan.TryGetComponent<BodyComponent>(player, out var bodyComp))
            return;

        var lewdSys = ctx.EntMan.System<LewdOrganSystem>();
        try
        {
            // Guaranteed to have a LewdOrgan as per above
            var organ = ctx.EntMan.Spawn(Organ, doMapInit: true);
            var organComp = ctx.EntMan.GetComponent<LewdOrganComponent>(organ);

            lewdSys.TryAddOrganToBody((organ, organComp), (player, bodyComp));
        }
        catch (Exception e)
        {
            Log.Error($"Exception while trying to add organ: {e}");
        }
    }
}
