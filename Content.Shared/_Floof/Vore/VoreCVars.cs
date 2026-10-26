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
    /// <summary>
    /// Enables or disables digestion verbs and interactions. 
    /// </summary>
    public static readonly CVarDef<bool> DigestionEnabled =
        CVarDef.Create("game.digestion_enabled", true, CVar.SERVER | CVar.REPLICATED);

    //client side cvars for customization
    public static readonly CVarDef<bool> AllowSound =
        CVarDef.Create("client.vore_allow_sound", false, CVar.CLIENTONLY | CVar.ARCHIVE);
    public static readonly CVarDef<float> DigestSpeed =
        CVarDef.Create("client.vore_digest_speed", 100f, CVar.CLIENTONLY | CVar.ARCHIVE);
    public static readonly CVarDef<string> StomachColor =
        CVarDef.Create("client.vore_stomach_color", "#59030F", CVar.CLIENTONLY | CVar.ARCHIVE);
}
