namespace Content.Shared._Floof.Ropes.Components;

/// <summary>
///     Added to ropes created by a rope connector.
/// </summary>
[RegisterComponent]
public sealed partial class RopeConnectorRopeComponent : Component
{
    [DataField]
    public EntityUid Connector;
}
