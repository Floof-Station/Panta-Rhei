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
    private SurgeryToolComponent.ToolTypeEnum _type = default!;

    [DataField("states")]
    private Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _states = new();

    [DataField("effects")]
    private Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _effects = new();

    [DataField]
    private ProtoId<OrganCategoryPrototype> remove = default!;

    [DataField]
    private ProtoId<OrganCategoryPrototype> insert = default!;


}
