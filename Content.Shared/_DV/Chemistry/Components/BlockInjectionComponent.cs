using Robust.Shared.GameStates;

namespace Content.Shared._DV.Chemistry.Components;

/// <summary>
/// Prevents injections being used on this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlockInjectionComponent : Component
{
    /// <summary>
    /// If true, this component will block injections from hypospray.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockHypospray;

    /// <summary>
    /// Reason why injections are blocked. Used for localization keys like "injector-component-deny-{BlockReason}".
    /// </summary>
    [DataField]
    public string ReasonLocId { get; set; } = "injector-component-deny-chitinid";
}
