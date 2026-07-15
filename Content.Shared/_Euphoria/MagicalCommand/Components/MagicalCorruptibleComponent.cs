using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.MagicalCommand.Components;

/// <summary>
/// This component designates that an item can be 'corrupted' into another using a valid tool.
/// Different from polymorphing because the  item is pre-defined in the component.
/// Currently used for the magical girl artifacts.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EntityId = "Crowbar";

    /// <summary>
    /// Whether the altering item needs <see cref="EmpoweredFriendshipComponent"/> to succeed.
    /// </summary>
    [DataField]
    public bool RequiresEmpowered;

    // If it's already corrupted, it needs to be decorrupted instead.
    [DataField, AutoNetworkedField]
    public bool Corrupted;

    [DataField]
    public SoundSpecifier CorruptStartSound = new SoundPathSpecifier("/Audio/_Euphoria/Magic/corruption-start.ogg");

    [DataField]
    public EntityUid? CorruptStream;

    [DataField]
    public SoundSpecifier CorruptFinishSound = new SoundPathSpecifier("/Audio/_Euphoria/Magic/corruption-crack.ogg");

    [DataField]
    public EntProtoId CorruptionStartEffect = "MagicalCommandEffectCorruptBegin";

    public EntityUid? CurrentEffect;

    [DataField]
    public EntProtoId CorruptionEndEffect = "MagicalCommandEffectCorruptEnd";
}
