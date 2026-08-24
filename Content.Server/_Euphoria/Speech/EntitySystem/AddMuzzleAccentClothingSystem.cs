using Content.Server._Euphoria.Speech.Components;
using Content.Server._Vulp.Speech.Accents.Mumble;
using Content.Server.Speech.Components;
using Content.Shared.Clothing;
using Robust.Shared.Prototypes;

namespace Content.Server._Euphoria.Speech.EntitySystem;

public sealed class AddMuzzleAccentClothingSystem : Robust.Shared.GameObjects.EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly MuzzledAccentSystem _muzzledSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddMuzzleAccentClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddMuzzleAccentClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<AddMuzzleAccentClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (HasComp<MuzzledAccentComponent>(args.Wearer)
            || !_protoMan.TryIndex(ent.Comp.Prototype, out var prototype))
            return;

        _muzzledSys.SetAccent(ent.Owner, prototype);
        ent.Comp.IsActive = true;
    }

    private void OnGotUnequipped(EntityUid uid, AddMuzzleAccentClothingComponent component, ref ClothingGotUnequippedEvent args)
    {
        if (!component.IsActive)
            return;

        RemComp<MuzzledAccentComponent>(args.Wearer);
        component.IsActive = false;
    }
}
