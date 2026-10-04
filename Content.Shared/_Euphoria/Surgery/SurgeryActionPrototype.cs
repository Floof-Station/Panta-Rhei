using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Surgery;

[Prototype("surgeryAction")]
public sealed partial class SurgeryActionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("toolType", required: true)]
    public SurgeryToolComponent.ToolTypeEnum _type = default!;

    [DataField("states")]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _states = new();

    [DataField("effects")]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _effects = new();

    [DataField]
    public ProtoId<OrganCategoryPrototype> remove = default!;

    [DataField]
    public ProtoId<OrganCategoryPrototype> insert = default!;


}
