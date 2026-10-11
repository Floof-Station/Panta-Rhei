using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Surgery;

/// <summary>
/// Contains what surgery qualities the tool has such as "cutting" or "retracting".
/// Maybe also put in how good the tool is as well
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryToolComponent : Component
{
    [ViewVariables]

    [DataField]
    public ToolTypeEnum ToolType = default;

    [DataField]
    public float Strength = 1;

    public enum ToolTypeEnum: int
    {
        Cutting = 1,
        Retracting = 2,
        Cauterising = 3,
        Sawing = 4,
        Drilling = 5,
    }

}
