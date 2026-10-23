using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Euphoria.Surgery;

[Serializable, NetSerializable]
public sealed class SurgeryUpdateState : BoundUserInterfaceState
{
    public List<ProtoId<SurgeryActionPrototype>> SurgeryActions;

    public SurgeryUpdateState(List<ProtoId<SurgeryActionPrototype>> recipes)
    {
        SurgeryActions = recipes;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryStartMessage : BoundUserInterfaceMessage
{
    public readonly string ID;

    public SurgeryStartMessage(string id)
    {
        ID = id;
    }
}
