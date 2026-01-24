using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Leash.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LeashedComponent : Component
{
    public const string VisualsContainerName = "leashed-visuals";

    /// <summary>
    ///     The joint ID of the leash joint. CAN BE NULL.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? JointId = null;

    [DataField, AutoNetworkedField]
    public NetEntity? Leash = null, Anchor = null;
}
