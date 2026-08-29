using Content.Server._Euphoria.Speech.Components;
using Content.Server._Vulp.Speech.Accents.Mumble;
using Content.Shared.Clothing;
using Robust.Shared.Prototypes;

namespace Content.Server._Euphoria.Speech.Systems;

public sealed class AddMuzzleAccentClothingSystem : EntitySystem
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

        _muzzledSys.SetAccent(args.Wearer, prototype);
        ent.Comp.IsActive = true;
    }

    private void OnGotUnequipped(Entity<AddMuzzleAccentClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!ent.Comp.IsActive)
            return;

        _muzzledSys.SetAccent(args.Wearer, null);
        ent.Comp.IsActive = false;
    }
}
