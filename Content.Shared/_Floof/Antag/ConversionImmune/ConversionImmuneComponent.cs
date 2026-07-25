using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Antag.ConversionImmune;

/// <summary>
///     Prevents the mob from being converted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConversionImmuneComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ConversionProtectionType ProtectionType;

    /// <summary>
    ///     If true, relevant antags will see an icon above the character telling them they are immune.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowIcon = true;

    [DataField, AutoNetworkedField]
    public ProtoId<FactionIconPrototype> ShownIcon = "ConversionImmune";
}

[Flags]
public enum ConversionProtectionType
{
    CosmicCult = 1 << 0, // When adding new ones, follow the flag pattern

    All = CosmicCult,
}
