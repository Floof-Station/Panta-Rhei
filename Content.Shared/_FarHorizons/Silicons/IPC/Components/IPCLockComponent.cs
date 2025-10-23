using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Silicons.IPC;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IPCLockComponent : Component
{
    [DataField]
    public string IDContainerID = "id";
    [DataField]
    [AutoNetworkedField]
    public bool InstantSelfUnlock = false;
    [DataField]
    [AutoNetworkedField]
    public bool InstantSelfLock = false;
    [DataField]
    [AutoNetworkedField]
    public TimeSpan LockTime;
    [DataField]
    [AutoNetworkedField]
    public TimeSpan UnlockTime;

    [DataField]
    [AutoNetworkedField]
    public bool LockSoundsEnabled = false;
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");
    [DataField]
    [AutoNetworkedField]
    public TimeSpan LockSoundsCooldown;
    [ViewVariables(VVAccess.ReadWrite)]
    public LockComponent Lock = default!;
    [ViewVariables(VVAccess.ReadWrite)]
    public WiresPanelComponent WiresPanel = default!;
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSound;
}

[Serializable, NetSerializable]
public sealed partial class IPCLockDoAfter : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class IPCUnlockDoAfter : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}