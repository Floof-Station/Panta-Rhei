using Content.Shared._DV.Traits.Effects;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class AddCTagTraitEffect : BaseTraitEffect
{
    /// <summary>
    /// The tag to add on to the entity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> tagsToAdd = new();

    public override void Apply(TraitEffectContext ctx)
    {
        ctx.EntMan.System<TagSystem>().AddTag(ctx.Player, tagsToAdd);
    }
}

