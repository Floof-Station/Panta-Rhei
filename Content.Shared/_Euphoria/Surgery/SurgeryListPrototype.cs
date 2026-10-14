using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Euphoria.Surgery;

/// <summary>
///     A container that allows for groups of surgery actions to be made.
/// </summary>
/// <remarks>
///     This is a wrapper that allows for groups of <see cref="SurgeryActionPrototype"/> to be assigned to mobs easier
///
/// </remarks>
[Prototype("surgeryList")]
public sealed partial class SurgeryListPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [ViewVariables]
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SurgeryActionPrototype>))]
    public string[]? Parents { get; private set; }

    [DataField]
    public List<ProtoId<SurgeryActionPrototype>> Actions = default!;
}
