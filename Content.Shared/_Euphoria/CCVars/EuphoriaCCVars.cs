using Robust.Shared.Configuration;

namespace Content.Shared._Euphoria.CCVars;

[CVarDefs]
public sealed partial class EuphoriaCCVars
{
    public static readonly CVarDef<bool> GreyNightVision =
        CVarDef.Create("accessibility.grey_night_vision", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
