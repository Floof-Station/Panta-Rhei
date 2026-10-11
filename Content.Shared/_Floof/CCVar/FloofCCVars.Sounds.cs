using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     Stores scent presets for the scent editor. Client-only.
    /// </summary>
    public static readonly CVarDef<bool> ChelpSoundEnabled =
        CVarDef.Create("audio.chelp_sound_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
