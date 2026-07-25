using Robust.Shared.Configuration;

namespace Content.Shared._Floof.Vore;

[CVarDefs]
public sealed class VoreCVars
{
    /// <summary>
    /// Enables or disables vore verbs and interactions.
    /// </summary>
    public static readonly CVarDef<bool> VoreEnabled =
        CVarDef.Create("game.vore_enabled", true, CVar.SERVER | CVar.REPLICATED);
}
