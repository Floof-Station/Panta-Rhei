using Content.Shared.Alert;
using Content.Shared.Ninja.Components;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Silicons.IPC;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IPCBatteryComponent : Component
{
    [DataField]
    public string BatteryContainerSlotID = "cell_slot";
    [DataField]
    public float DieWithoutPowerAfter = 30f;
    [DataField]
    public int NumWarnings = 0;
    [DataField]
    public LocId? WarningText = null;
    [DataField]
    public SoundSpecifier? WarningSound = null;
    [DataField]
    public ProtoId<AlertPrototype> ChargeCritical = "IPCBatteryCrit";
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot BatteryContainerSlot = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public BatteryDrainerComponent BatteryDrainer = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public PowerCellSlotComponent PowerCellSlot = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public EntityUid? Battery = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool TimerActive = false;
    [ViewVariables(VVAccess.ReadWrite)]
    public float Timer = 0f;
    [ViewVariables(VVAccess.ReadWrite)]
    public int WarningsIssued = 0;
}