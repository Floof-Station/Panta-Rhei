using Content.Shared.Humanoid;

namespace Content.Shared._Floof.Humanoid.ModifyUndies;


[RegisterComponent]
public sealed partial class ModifyUndiesComponent : Component
{
    /// <summary>
    ///     The bodypart target enums for the undies.
    /// </summary>
    public List<HumanoidVisualLayers> BodyPartTargets =
        new()
        {
            HumanoidVisualLayers.UndergarmentBottom,
            HumanoidVisualLayers.UndergarmentTop
        };
}


