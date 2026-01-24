using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared._Floof.CCVar;
using Content.Shared.Administration;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Client._Floof.Leash.Commands;

[AnyCommand]
public sealed class LeashPredictCommand : IConsoleCommand
{
    public string Command => "leashpredict";
    public string Description => "Toggle whether to predict leashes. This can cause visual artifacts when you are the one being leashed or holding the leash." +
                                 "You may have to recreate the leash joint in order to see the effects.";
    public string Help => Command;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var config = IoCManager.Resolve<IConfigurationManager>();
        var curr = config.GetCVar(FloofCCVars.PredictLeashes);
        config.SetCVar(FloofCCVars.PredictLeashes, !curr);

        shell.WriteLine($"Leash prediction is now {(curr ? "disabled" : "enabled")}.");
    }
}
