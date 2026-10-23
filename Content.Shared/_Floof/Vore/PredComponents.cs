using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
namespace Content.Shared._Floof.Vore;

/// <summary>
/// Handling of predator component for customization
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PredComponent : Component
{
    /// The ID of the container used for vore mechanics.
    //TODO later include customizable containers for different vore types
    [DataField("containerId")]
    public string ContainerId = "vore_container";
    [DataField]
    public SoundSpecifier SoundDevour = new SoundPathSpecifier("/Audio/_Floof/Vore/gulp.ogg");
    [DataField("digestSpeed")]
    public float DigestSpeed = 100f;
    [DataField("stomachColor"), AutoNetworkedField]
    public Color StomachColor = new(0.35f, 0.01f, 0.06f);
}

/// <summary>
/// Event raised when a prey is devoured 
/// </summary>
[Serializable, NetSerializable]
public sealed partial class OnVoreDoAfter : SimpleDoAfterEvent{
    /// Harcoded max prey for balancing purpose
    [DataField("maxPrey")]
    public int MaxPrey = 3;
    public OnVoreDoAfter(int maxPrey)
    {
        MaxPrey = maxPrey;
    }
}