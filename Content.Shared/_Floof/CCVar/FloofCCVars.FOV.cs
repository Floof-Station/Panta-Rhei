using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     The number by which the current FOV size is mulltiplied when zooming in.
    /// </summary>
    public static readonly CVarDef<float> ZoomInStep =
        CVarDef.Create("fov.zoom_in_step", 0.8f, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    ///     The number by which the current FOV size is multiplied when zooming out.
    /// </summary>
    public static readonly CVarDef<float> ZoomOutStep =
        CVarDef.Create("fov.zoom_out_step", 1.25f, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);
}
