using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     Whether to walk by default instead of running. Set on the client side and replicated to server.
    /// </summary>
    public static readonly CVarDef<bool> DefaultWalk =
        CVarDef.Create("game.default_walk", false, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);
}
