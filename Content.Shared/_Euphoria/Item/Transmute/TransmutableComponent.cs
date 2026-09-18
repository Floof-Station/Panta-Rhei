using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Euphoria.Item.Transmute;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TransmutableComponent : Component
{
    /// <summary>
    /// List of prototypes that the item can be transmuted into
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> AvailablePrototypes { get; set; } = new();

    /// <summary>
    /// List of components to keep when transmuting
    /// </summary>
    [DataField(customTypeSerializer: typeof(CustomArraySerializer<string, ComponentNameSerializer>))]
    public string[]? KeepComponents;

    /// <summary>
    /// Sound that plays on transmutation
    /// </summary>
    [DataField]
    public SoundSpecifier TransmuteSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    /// <summary>
    /// Whether to play a sound on transmuting
    /// </summary>
    [DataField]
    public bool PlayTransmuteSound { get; set; } = true;

    /// <summary>
    /// The time taken to transmute
    /// </summary>
    [DataField]
    public float Delay { get; private set; } = 0f;
}
