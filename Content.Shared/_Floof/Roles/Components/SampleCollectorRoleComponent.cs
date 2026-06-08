using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a sample collector.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SampleCollectorRoleComponent : BaseMindRoleComponent;
