using Content.Shared._Vulp.Speech.Accents.Mumble;
using Robust.Shared.Prototypes;

namespace Content.Server._Euphoria.Speech.Components;

/// <summary>
///     A version of AddAccentClothing that adds a muzzle accent.
/// </summary>
[RegisterComponent]
public sealed partial class AddMuzzleAccentClothingComponent : Component
{
    [DataField]
    public ProtoId<MuzzleAccentPrototype> Prototype;

    /// <summary>
    ///     Is that clothing is worn and affecting someones accent?
    /// </summary>
    [ViewVariables]
    public bool IsActive = false;
}
