using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     If true, full language names will be displayed in chat instead of just an icon
    /// </summary>
    public static readonly CVarDef<bool> AccessibilityFullLanguageNames =
        CVarDef.Create("accessibility.full_language_names", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
