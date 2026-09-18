using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Attached to an entity after portal transit to mark that they should not immediately be portaled back
///     at the end destination.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PortalTimeoutComponent : Component
{
    /// <summary>
    ///     The portal that was entered. Null if coming from a hand teleporter, etc.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public EntityUid? EnteredPortal;
}

/// <summary>
///     Euph. Prevents this entity from entering portals at all.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PortalBlockComponent : Component;

