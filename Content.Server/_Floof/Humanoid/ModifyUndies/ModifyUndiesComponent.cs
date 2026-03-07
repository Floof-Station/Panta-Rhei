using Content.Shared.Humanoid;

namespace Content.Server._Floof.Humanoid.ModifyUndies;


/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ModifyUndiesComponent : Component
{
    /// <summary>
    ///     The bodypart target enums for the undies.
    /// </summary>
    public List<HumanoidVisualLayers> BodyPartTargets =
    [
        HumanoidVisualLayers.UndergarmentBottom,
        HumanoidVisualLayers.UndergarmentTop
    ];
}


