using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.Surgery;

/// <summary>
/// Contains what surgery qualities the tool has such as "cutting" or "retracting".
/// The stronger the tool, the faster the surgery. This also contains the sounds for the tool.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryToolComponent : Component
{
    [ViewVariables]

    [DataField]
    public ToolTypeEnum ToolType = default;

    [DataField]
    public float Strength = 1;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EndSound;

    //This specifies who the tool was last used on. Currently unused functionally.
    //Could be used for cross contamination logic.
    [DataField]
    public EntityUid? LastUsedOn = null;

    public enum ToolTypeEnum: int
    {
        Cutting = 1,
        Retracting = 2,
        Cauterising = 3,
        Sawing = 4,
        Drilling = 5,
    }

}
