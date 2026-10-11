using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    public static readonly CVarDef<bool> WeatherCycleEnabled =
        CVarDef.Create("weather.cycle_enabled", true, CVar.SERVERONLY);
}
