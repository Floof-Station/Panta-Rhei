using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Floof.Chat.Commands;

[AnyCommand]
internal sealed class SubtleOOCCommand : IConsoleCommand
{
    public string Command => "sooc";
    public string Description => "Send an out-of-character message in whisper range. The message won't go through walls and won't be seen by ghosts (except admins).";
    public string Help => "sooc <text>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not {} playerEntity)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>()
            .TrySendInGameOOCMessage(playerEntity, message, InGameOOCChatType.SubtleLOOC, false, shell, player);
    }
}
