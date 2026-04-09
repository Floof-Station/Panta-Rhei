using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     Whether or not to run the scent detection system. Toggle if server performance gets shitty.
    /// </summary>
    public static readonly CVarDef<bool> ScentDectectionToggle =
        CVarDef.Create("game.do_scent_detection", true, CVar.SERVER | CVar.REPLICATED);
}
