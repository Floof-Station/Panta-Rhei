namespace Content.Server._Floof.HeightAdjust.Components;

/// <summary>
///     Indicates that the height of this humanoid should affect the radii of its fixtures.
///     Has no effects on non-humanoids.
/// </summary>
[RegisterComponent]
public sealed partial class FixturesAffectedByHeightComponent : Component
{
}
