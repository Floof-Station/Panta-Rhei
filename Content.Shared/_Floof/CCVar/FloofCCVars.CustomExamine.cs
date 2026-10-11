using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    /// List of custom examine presets.
    /// </summary>
    public static readonly CVarDef<string> CustomExaminePresets =
        CVarDef.Create("client.custom_examine_presets",
        "",
        CVar.CLIENTONLY | CVar.ARCHIVE);
}
