using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Surgery;

public sealed partial class SurgeryOrganStatus
{
    [DataField]
    public EntityUid Organ { get; set; }

    [DataField]
    public Dictionary<ProtoId<SurgeryStatePrototype>,bool> _status = new();

    public SurgeryOrganStatus(EntityUid organ)
    {
        Organ = organ;
    }
}
