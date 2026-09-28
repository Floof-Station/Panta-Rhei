using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Silicons.IPC.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedIPCSystem))]
public sealed partial class IPCModulesComponent : Component
{
    /// <summary>
    /// The ID for the module container.
    /// </summary>
    [DataField]
    public string ModuleContainerId = "borg_module";

    /// <summary>
    /// The currently selected module.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SelectedModule;
}