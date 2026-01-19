using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Floof.Language.Commands;

[AnyCommand]
public sealed class SayLanguageCommand : IConsoleCommand
{
    public string Command => "saylang";
    public string Description => Loc.GetString("command-saylang-desc");
    public string Help => Loc.GetString("command-saylang-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not {} playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (args.Length < 2)
            return;

        var message = string.Join(" ", args, startIndex: 1, count: args.Length - 1).Trim();

        if (string.IsNullOrEmpty(message))
            return;

        var languages = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();
        var chats = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();

        if (!SelectLanguageCommand.TryParseLanguageArgument(languages, playerEntity, args[0], out var failReason, out var language))
        {
            shell.WriteError(failReason);
            return;
        }

        // Floofstation: this system used to use a "language override" provided by the chat system.
        // This is no longer the case. Instead, we temporarily modify the entity's current language.
        using var _ = languages.SubstituteEntityLanguage(playerEntity, language.ID);
        chats.TrySendInGameICMessage(playerEntity, message, InGameICChatType.Speak, ChatTransmitRange.Normal, false, shell, player);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args) =>
        args.Length switch
        {
            0 or 1 => SelectLanguageCommand.GetLanguageIdCompletions(shell, false), // Saylang doesn't support numeric arguments
            _ => CompletionResult.FromHint("Message...")
        };
}
