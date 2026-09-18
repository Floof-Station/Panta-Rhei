using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Leash.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LeashedComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Leash = null, Anchor = null;
}
