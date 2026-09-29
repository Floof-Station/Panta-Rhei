using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Euphoria.Surgery;

[Serializable, NetSerializable]
public enum SurgeryUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SurgeryBuiState(Dictionary<NetEntity, List<EntProtoId>> choices) : BoundUserInterfaceState
{
    public readonly Dictionary<NetEntity, List<EntProtoId>> Choices = choices;
}

[Serializable, NetSerializable]
public sealed class SurgeryBuiRefreshMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class SurgeryStepChosenBuiMsg(NetEntity part, EntProtoId action, bool isBody) : BoundUserInterfaceMessage
{
    public readonly NetEntity Part = part;
    public readonly EntProtoId SurgeryAction = action;

    // Used as a marker for whether or not we're hijacking surgery by applying it on the body itself.
    public readonly bool IsBody = isBody;
}
