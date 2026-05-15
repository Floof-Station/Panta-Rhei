using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.Item.AttunableItem;

/// <summary>
/// This component makes it so that an entity can 'attune' to the object.
/// Only attuned entities can use the object.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AttunableEquipmentComponent : Component
{
    /// <summary>
    /// The thing this item is attuned to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AttunedEnt;
}
