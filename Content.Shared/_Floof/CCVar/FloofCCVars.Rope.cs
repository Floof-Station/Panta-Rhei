using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     If true, entities going through portals will pull all attached entities with them. Disable only if it causes major physics bugs.
    /// </summary>
    public static readonly CVarDef<bool> RopesFollowPortals =
        CVarDef.Create("rope.follow_portals", true, CVar.SERVER | CVar.REPLICATED);
}
