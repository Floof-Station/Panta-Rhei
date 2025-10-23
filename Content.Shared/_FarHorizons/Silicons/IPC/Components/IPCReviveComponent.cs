using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Silicons.IPC;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IPCReviveComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool RebootButton = false;
    
    [DataField]
    [AutoNetworkedField]
    public TimeSpan RebootTime;

    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? RebootSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? RebootFailSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");

    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? RebootSuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [DataField]
    [AutoNetworkedField]
    public LocId CantReviveMessage = "ipc-revive-cant-revive";

    [DataField]
    [AutoNetworkedField]
    public LocId RebootingMessage = "ipc-revive-reboot-started";

    [DataField]
    [AutoNetworkedField]
    public DamageSpecifier? DefibDamage = null;

    [DataField]
    [AutoNetworkedField]
    public bool DefibBatteryDrain = false;
}

[Serializable, NetSerializable]
public sealed partial class IPCRebootDoAfterEvent : SimpleDoAfterEvent;