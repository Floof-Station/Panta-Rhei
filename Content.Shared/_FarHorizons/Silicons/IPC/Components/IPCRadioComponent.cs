using Content.Shared.Radio.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._FarHorizons.Silicons.IPC.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IPCRadioComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CopyHeadsetKeys = false;
    [DataField("removeHeadset"), AutoNetworkedField]
    public bool RemoveHeadsetOnRoundstart = false;
    [DataField, AutoNetworkedField]
    public int KeysCapacity = 0;

    [DataField, AutoNetworkedField]
    public string EncryptionKeysContainerID = "key_slots";
    [DataField, AutoNetworkedField]
    public string HeadsetContainerID = "ears";
    [DataField, AutoNetworkedField]
    public SoundSpecifier KeyInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");
    [DataField, AutoNetworkedField]
    public SoundSpecifier KeyExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    public Container EncryptionKeysContainer = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public IntrinsicRadioTransmitterComponent RadioTransmitter = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public ActiveRadioComponent RadioReceiver = default!;
}