using Content.Shared._DV.Traits.Effects;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Traits.Effects;

public sealed partial class AddTagTraitEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> tagsToAdd = new();

    public override void Apply(TraitEffectContext ctx)
    {
        ctx.EntMan.System<TagSystem>().AddTag(ctx.Player, tagsToAdd);
    }
}
