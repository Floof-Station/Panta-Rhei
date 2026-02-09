using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     Whether StationPlanetSpawner can spawn planets (lavaland). Enabled by default, disabled in development.
    /// </summary>
    public static readonly CVarDef<bool> StationPlanetSpawning =
        CVarDef.Create("game.station_planet_spawning", true, CVar.ARCHIVE | CVar.SERVERONLY);
}
