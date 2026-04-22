using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Specifies the selected prototype and custom data for a loadout.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class Loadout : IEquatable<Loadout>
{
    [DataField]
    public ProtoId<LoadoutPrototype> Prototype;

    // Floofstation section
    /// <summary>
    ///     Metadata overrides for the entity. Color is hex-encoded.
    /// </summary>
    [DataField]
    public string? NameOverride, DescriptionOverride, ColorOverride;

    public bool HasCustomMetadata => NameOverride != null || DescriptionOverride != null || ColorOverride != null;
    // Floofstation section end

    // Floofstation section - constructors
    public Loadout() {}

    public Loadout(Loadout copy)
    {
        Prototype = copy.Prototype;
        NameOverride = copy.NameOverride;
        DescriptionOverride = copy.DescriptionOverride;
        ColorOverride = copy.ColorOverride;
    }
    // Floofstation section end

    public bool Equals(Loadout? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        // Floofstation section
        if (NameOverride != other.NameOverride) return false;
        if (DescriptionOverride != other.DescriptionOverride) return false;
        if (ColorOverride != other.ColorOverride) return false;
        // Floofstation section end
        return Prototype.Equals(other.Prototype);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Loadout other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Prototype.GetHashCode(), NameOverride?.GetHashCode() ?? 0, DescriptionOverride?.GetHashCode() ?? 0, ColorOverride?.GetHashCode() ?? 0); // Floofstation
    }
}
