using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Ropes.Components;

/// <summary>
///     Added to entities that have had a rope tied to them using the <see cref="RopeConnectorComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RopeConnectorAttachedComponent : Component
{
    /// <summary>
    ///     After a connector was used to attach both ends of a rope, its handle is placed inside a container with this name, attached to the end anchor.
    /// </summary>
    public static string ConnectorContainer = "rope-connector";

    /// <summary>
    ///     The connector used to attach the rope.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Connector;

    /// <summary>
    ///     Which side of the rope is attached here.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RopeSide Side;

    [DataField, AutoNetworkedField]
    public bool CanDetach = true;

    [DataField, AutoNetworkedField]
    public TimeSpan DetachDelay = TimeSpan.FromSeconds(3);
}
