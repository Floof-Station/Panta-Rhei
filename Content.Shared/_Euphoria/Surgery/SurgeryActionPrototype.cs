using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Euphoria.Surgery;

[Prototype("surgeryAction")]
public sealed partial class SurgeryActionPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables]
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SurgeryActionPrototype>))]
    public string[]? Parents { get; private set; }

    [DataField("toolType")]
    public SurgeryToolComponent.ToolTypeEnum _type = default!;

    [DataField("icon")]
    public SpriteSpecifier? Icon = default!;

    [DataField("duration")]
    public float _duration;

    [DataField("states")]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _states = new();

    [DataField("effects")]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _effects = new();

    [DataField]
    public ProtoId<OrganCategoryPrototype>? Remove;

    [DataField]
    public ProtoId<OrganCategoryPrototype>? Insert;

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }


}
