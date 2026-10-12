using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Euphoria.Surgery;

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
