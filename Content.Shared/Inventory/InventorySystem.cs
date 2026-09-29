using Content.Shared.Hands.Components;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{

    public override void Initialize()
    {
        base.Initialize();
        InitializeEquip();
        InitializeRelay();
        InitializeSlots();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownSlots();
    }

    // Euphoria - Used with SetInventoryEffect so that we can override
    // species inventory templates for things like gas masks
     public void SetSpeciesId(Entity<InventoryComponent> entity, string? speciesId)
    {
        entity.Comp.SpeciesId = speciesId;
        Dirty(entity);
    }
}
