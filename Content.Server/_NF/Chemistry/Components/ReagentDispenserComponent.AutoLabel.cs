namespace Content.Server.Chemistry.Components;

/// <summary>
/// Frontier - Extends ReagentDispenserComponent.
///
/// Used primarily for the auto-labeling functionality.
/// </summary>
public sealed partial class ReagentDispenserComponent : Component
{
    /// <summary>
    /// Returns if the component's entity has the ability to auto-label.
    /// </summary>
    [DataField]
    public bool CanAutoLabel = true;

    /// <summary>
    /// Returns if the entity has auto-labeling toggled on.
    /// Will have no effect if <see cref="CanAutoLabel"/> is false.
    /// </summary>
    [DataField, ViewVariables] // Floofstation - make this a datafield so we can turn this off
    public bool AutoLabelToggle = true;
}
