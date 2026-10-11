using Robust.Shared.Map;

namespace Content.Server.Antag.Components;

/// <summary>
/// Spawns this rule's antags on a station. It will attempt to
/// obtain and return the coordinates for latejoin spawnpoints and choose one at random.
/// Failing that, it will attempt to obtain any tile on a station.
/// Requires <see cref="AntagSelectionComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class AntagStationSpawnRuleComponent : Component
{
    /// <summary>
    /// Location that was picked.
    /// </summary>
    public EntityCoordinates? Coords;
}
