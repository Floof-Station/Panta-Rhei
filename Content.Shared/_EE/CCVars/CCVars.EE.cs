using Robust.Shared.Configuration;

namespace Content.Shared._EE.CCVars;

[CVarDefs]
public sealed partial class EECVars
{
    /// <summary>
    ///     How many lines back in the chat log to look for collapsing repeated messages into one.
    /// </summary>
    public static readonly CVarDef<int> ChatStackLastLines =
        CVarDef.Create("chat.chatstack_last_lines", 1, CVar.CLIENTONLY | CVar.ARCHIVE, "How far into the chat history to look when looking for similiar messages to coalesce them.");

    /// <summary>
    ///     Enables station goals
    /// </summary>
    public static readonly CVarDef<bool> StationGoalsEnabled =
        CVarDef.Create("game.station_goals", true, CVar.SERVERONLY);

    /// <summary>
    ///     Chance for a station goal to be sent
    /// </summary>
    public static readonly CVarDef<float> StationGoalsChance =
        CVarDef.Create("game.station_goals_chance", 0.25f, CVar.SERVERONLY);
}
