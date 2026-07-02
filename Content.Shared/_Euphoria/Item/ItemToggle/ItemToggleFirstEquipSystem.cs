using Content.Shared.Clothing.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared._Euphoria.Item.ItemToggle;

public sealed class ItemToggleFirstEquipSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemToggleFirstEquipComponent, ClothingEquipDoAfterEvent>(OnClothingEquipped);
    }

    private void OnClothingEquipped(Entity<ItemToggleFirstEquipComponent> ent, ref ClothingEquipDoAfterEvent args)
    {
        if (!TryComp<ItemToggleComponent>(ent, out var toggleComp) || toggleComp.Activated)
            return;

        _toggle.Toggle((ent, toggleComp), null, toggleComp.Activated, false);
    }
}
