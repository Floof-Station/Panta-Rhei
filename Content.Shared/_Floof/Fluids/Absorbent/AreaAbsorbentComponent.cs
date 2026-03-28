using Content.Shared._EE.FootPrint;
using Content.Shared.Whitelist;

namespace Content.Shared._Floof.Fluids.Absorbent;

/// <summary>
///     Absorbent that can clean up nearby puddles with an AOE effect. Requires a normal absorbent component to function.
/// </summary>
[RegisterComponent]
public sealed partial class AreaAbsorbentComponent : Component
{
    [DataField]
    public EntityWhitelist CleaningWhitelist = new() { Components = new[]{ "FootPrint" } };

    [DataField]
    public float CleaningRange = 0.2f;

    /// <summary>
    ///     How many puddles can be cleaned at once.
    /// </summary>
    [DataField]
    public int MaxCleanedEntities = 5;
}
