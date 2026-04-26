using Robust.Shared.Configuration;

namespace Content.Shared._Floof.CCVar;

public sealed partial class FloofCCVars
{
    /// <summary>
    ///     Whether or not to run the scent detection system. Toggle if server performance gets shitty.
    /// </summary>
    public static readonly CVarDef<bool> ScentDectectionToggle =
        CVarDef.Create("game.do_scent_detection", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Stores scent presets for the scent editor. Client-only.
    /// </summary>
    /// <seealso cref="Content.Client.ScentEditorDialog.ScentPreset"/>
    /// <remarks>Examples are "unwashed fox" and "dog washed with aloe shampoo". For the funny because this system is making me go want to pull my fucking hair out, and this is the first time into the 3 hour coding session that I can feel some relief.</remarks>
    public static readonly CVarDef<string> ScentPresets =
        CVarDef.Create("client.scent_presets", "Example,UnwashedFox;Example2,WetDog,Aloe", CVar.CLIENTONLY | CVar.ARCHIVE);
}
