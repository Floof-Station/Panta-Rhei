using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a food critic.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FoodCriticRoleComponent : BaseMindRoleComponent;
