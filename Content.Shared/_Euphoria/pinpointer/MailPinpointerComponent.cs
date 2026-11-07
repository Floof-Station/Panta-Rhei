using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.MailPinpointer;

/// <summary>
/// Component for Mail trackers.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MailPinpointerComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier UseSuccess = new SoundPathSpecifier("/Audio/Machines/beep.ogg", AudioParams.Default.WithVolume(-4f));
    [DataField, AutoNetworkedField]
    public SoundSpecifier UseFail = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg", AudioParams.Default.WithVolume(-6f));
    [DataField, AutoNetworkedField]
    public SoundSpecifier UseDeny = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg", AudioParams.Default.WithVolume(-6f));
}
