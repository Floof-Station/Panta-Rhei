using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Tools.Components;

/// <summary>
/// This component designates that an item can be 'corrupted' into another using a valid tool.
/// Different from polymorphing because the  item is pre-defined in the component.
/// Currently used for the magical girl artifacts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CorruptibleComponent : Component
{
    /// <summary>
    ///     The amount of time (in seconds) it takes to corrupt an item.
    /// </summary>
    [DataField]
    public float Time = 3f;

    /// <summary>
    /// The item that this will turn into.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EntityId;

    // If it's already corrupted, it needs to be decorrupted instead.
    [DataField]
    public bool Corrupted;

    [DataField]
    public SoundSpecifier CorruptStartSound = new SoundPathSpecifier("/Audio/_DV/Effects/clang2.ogg");

    [DataField]
    public EntityUid? CorruptStream;

    [DataField]
    public SoundSpecifier CorruptFinishSound = new SoundPathSpecifier("/Audio/_Euphoria/Magic/magical-transformation-evil.ogg");
}
