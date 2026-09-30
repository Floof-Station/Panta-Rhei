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
    public int Type = (int)ToolType.Cutting;

    [DataField]
    public int Strength = 0;

    public enum ToolType: int
    {
        Cutting = 1,
        Retracting = 2,
        Cauterising = 3,
        Sawing = 4
    }

}
