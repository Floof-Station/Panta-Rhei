namespace Content.Server._Floof.HeightAdjust.Components;

/// <summary>
///     Monitors the change of height of this humanoid and notifies other HeightAdjust systems whenever it changes.
/// </summary>
[RegisterComponent]
public sealed partial class HumanoidHeightMonitoringComponent : Component
{
    [DataField]
    public float LastHeight = 1f;
}
