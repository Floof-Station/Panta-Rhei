using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     Client-only, defines whether the client should predict leash joints.
    /// </summary>
    public static readonly CVarDef<bool> PredictLeashes =
        CVarDef.Create("leash.predict", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
