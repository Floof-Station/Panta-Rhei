using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.CoolingUnit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CoolingUnitComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleCoolingUnit";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    /// Maximum heat removed per second.
    /// </summary>
    [DataField]
    public float MaxCooling = 12f;
}
