using Content.Shared.Item.ItemToggle;
using Robust.Shared.Random;

namespace Content.Shared._Euphoria.Item.ItemToggle;

public sealed class ItemToggleRandomToggleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleRandomToggleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ItemToggleRandomToggleComponent> ent, ref MapInitEvent args)
    {
        if (!_random.Prob(ent.Comp.Chance))
            return;

        _itemToggle.Toggle(ent.Owner, showPopup: false);
    }
}
