using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Lewd;

/// <summary>
///     This is only used for organ mapping and does not actually dictate what reagents are produced.
/// </summary>
/// <remarks>This IS a flag enum, but you should never give an organ more than one organ type. Doing so will result in undefined behavior.</remarks>
[Serializable, NetSerializable, Flags]
public enum LewdOrganKind
{
    Breasts = 1,
    Penis   = 1 << 1,
    Vagina  = 1 << 2,
    Rectum  = 1 << 3,

    None    = 0,
    All     = Breasts | Penis | Vagina | Rectum, // Traits use this for targetting
    TotalCount = 4, // Update this when adding more
}
