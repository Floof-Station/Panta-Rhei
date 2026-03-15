namespace Content.Shared._Floof.Traits.Components;

/// <summary>
///     Marks this player as eligible for being the target of
///     chosen types of antagonist objectives.
/// </summary>
[RegisterComponent]
public sealed partial class MarkedComponent : Component
{
    [DataField]
    public ObjectiveTypes TargetType;
}

//Euphoria Target Consent Traits: Start
[Flags]
public enum ObjectiveTypes
{
    Unspecified = 0,
    Remove = 1 << 0,
    Teach = 1 << 1
}
//Euphoria Target Consent Traits: End
