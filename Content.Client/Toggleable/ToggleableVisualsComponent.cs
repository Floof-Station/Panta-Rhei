using Content.Shared.Hands.Components;

namespace Content.Client.Toggleable;

/// <summary>
/// Component that handles toggling the visuals of an entity, including layers on an entity's sprite,
/// the in-hand visuals, and the clothing/equipment visuals.
/// </summary>
/// <see cref="ToggleableVisualsSystem"/>
[RegisterComponent]
public sealed partial class ToggleableVisualsComponent : Component
{
    /// <summary>
    /// Sprite layer that will have its visibility toggled when this item is toggled.
    /// </summary>
    [DataField(required: true)]
    public string? SpriteLayer;

    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    /// <summary>
    /// Layers to add to the sprite of the player that is wearing this entity (while the component is toggled on).
    /// </summary>
    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    // Begin Euphoria changes

    /// <summary>
    /// Determines whether the layer is just added and removed or whether it overwrites the previous appearance entirely.
    /// </summary>
    [DataField]
    public bool ReplaceInHands;

    [DataField]
    public string ToggledKeyword = "toggled";

    //End Euphoria changes
}
