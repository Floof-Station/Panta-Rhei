using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
namespace Content.Shared._Floof.Vore;

[RegisterComponent]
public sealed partial class VoreComponent : Component
{
    /// <summary>
    /// The ID of the container used for vore mechanics.
    /// </summary>
    //TODO later include customizable containers for different vore types
    [DataField("containerId")]
    public string ContainerId = "vore_container";
    [DataField]
    public SoundSpecifier SoundDevour = new SoundPathSpecifier("/Audio/_Floof/Vore/gulp.ogg");
}
[Serializable, NetSerializable]
public sealed partial class OnVoreDoAfter : SimpleDoAfterEvent{
    /// <summary>
    /// Maximum number of prey this entity can hold.
    /// </summary>
    [DataField("maxPrey")]
    public int MaxPrey = 3;
    public OnVoreDoAfter(int maxPrey)
    {
        MaxPrey = maxPrey;
    }
}