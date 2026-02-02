using System.Numerics;

namespace Content.Shared._Floof.Leash.Components;

/// <summary>
///     Indicates that this entity or the entity that wears this entity can be leashed.
/// </summary>
[RegisterComponent]
public sealed partial class LeashAnchorComponent : Component
{
    /// <summary>
    ///     The visual offset of the "anchor point".
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    [DataField]
    public AnchorKind Kind = AnchorKind.Any;

    [Flags]
    public enum AnchorKind : int
    {
        /// <summary>This entity is a clothing that, when equipped, can have a leash attached to it.</summary>
        Clothing,
        /// <summary>This entity can have a leash attached to it normally.</summary>
        Intrinsic,

        Any = Clothing | Intrinsic
    }
}
