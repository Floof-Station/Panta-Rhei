using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.NPC.Systems;
using Robust.Shared.Toolshed;

namespace Content.Server._Floof.NPC.Commands;

[ToolshedCommand(Name = "faction"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminFactionCommand : ToolshedCommand
{
    private NpcFactionSystem? _factionField;
    private NpcFactionSystem Factions => _factionField ??= GetSys<NpcFactionSystem>();

    [CommandImplementation("add")]
    public EntityUid AddFaction(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] string faction
    )
    {
        Factions.AddFaction(input, faction);

        return input;
    }

    [CommandImplementation("rm")]
    public EntityUid RmFaction(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] string faction
    )
    {
        Factions.RemoveFaction(input, faction);

        return input;
    }

    [CommandImplementation("clear")]
    public EntityUid ClearFaction(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input
    )
    {
        Factions.ClearFactions(input);

        return input;
    }

}
