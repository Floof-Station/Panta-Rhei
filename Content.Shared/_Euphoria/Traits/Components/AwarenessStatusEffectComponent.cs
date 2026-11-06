using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.Traits.Components;

/// <summary>
/// Stores information for awareness status effects
/// Has to be applied as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AwarenessStatusEffectComponent : Component
{
    [DataField]
    public AwarenessTypes AwarenessType;

    //Add any future effects in powers of two
    [Flags]
    public enum AwarenessTypes
    {
        Hunger = 1,
        Thirst = 2,
    }
}
