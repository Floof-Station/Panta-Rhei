using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.Item.ItemToggle;

/// <summary>
///     Randomly toggles an item with a <see cref="ItemToggleComponent"/> on map init.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemToggleFirstEquipComponent : Component
{
}
