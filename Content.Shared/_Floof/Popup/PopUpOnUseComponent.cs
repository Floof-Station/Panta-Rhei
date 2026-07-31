using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Popup;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PopUpOnUseComponent : Component
{
    /// <summary>
    /// The popup message when successfully finishing a do after on self
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId UserPopUpMsg = "self-popup-on-use-success";

    /// <summary>
    /// The popup message when successfully finishing a do after on a target
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId TargetPopUpMsg = "target-popup-on-use-success";

    /// <summary>
    /// The sound to play when a do after is starting
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? PopUpDoAfterSound;

    /// <summary>
    /// How long it takes to complete the do after
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PopUpDoAfterTime = TimeSpan.Zero;

    // Used to cancel the played sound.
    public EntityUid? AudioStream;

    // Fishy whitelists
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Blacklist;
}
