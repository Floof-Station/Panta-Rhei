using Content.Shared._DV.Traits.Effects;
using Content.Shared.Inventory;

namespace Content.Shared._Euphoria.Traits.Effects;

/// <summary>
/// Sets the species ID used by the player's inventory for species-specific clothing visuals.
/// </summary>
public sealed partial class SetInventoryEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public string SpeciesId;

    public override void Apply(TraitEffectContext ctx)
    {
        var inventory = ctx.EntMan.GetComponent<InventoryComponent>(ctx.Player);
        ctx.EntMan.System<InventorySystem>().SetSpeciesId((ctx.Player, inventory), SpeciesId);
    }
}
