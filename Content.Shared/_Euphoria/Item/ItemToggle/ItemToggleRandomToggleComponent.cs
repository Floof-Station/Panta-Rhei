using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.Item.ItemToggle;

/// <summary>
///     Randomly toggles an item with a <see cref="ItemToggleComponent"/> on map init.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleRandomToggleComponent : Component
{
    /// <summary>
    ///     The chance that the item will be toggled on map init.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Chance = 0.1f;
}
