using Content.Shared._Vulp.Speech.Accents.Mumble;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;


namespace Content.Server._Vulp.Speech.Accents.Mumble;


[RegisterComponent]
public sealed partial class MuzzledAccentComponent : Component
{
    /// <summary>
    ///     The accent to apply to the entity. Do not modify directly unless the component is non-map-initialized.
    /// </summary>
    [DataField(required: true), Access(typeof(MuzzledAccentSystem), Other = AccessPermissions.Read)]
    public ProtoId<MuzzleAccentPrototype> Accent = "BasicMuzzle";

    /// <summary>
    ///     The loaded accent prototype.
    /// </summary>
    [NonSerialized, ViewVariables]
    public MuzzleAccentPrototype? AccentPrototype;

    /// <summary>
    /// This modifies the audio parameters of emote sounds, screaming, laughing, etc.
    /// By default, it reduces the volume and distance of emote sounds.
    /// </summary>
    [DataField]
    public AudioParams EmoteAudioParams = AudioParams.Default.WithVolume(-8f).WithMaxDistance(5);
}
