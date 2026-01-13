using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Floofstation-specific stuff
/// </summary>
public sealed partial class ChatSystem
{
    private void SendEntitySubtle(
        EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        NetUserId? author = null)
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        // get the entity's apparent name (if no override provided).
        var ent = Identity.Entity(source, EntityManager);
        string name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));

        // Emotes use Identity.Name, since it doesn't actually involve your voice at all.
        var wrappedMessage = Loc.GetString("chat-manager-entity-subtle-wrap-message",
            ("entityName", name),
            ("entity", ent),
            ("message", action)); // Floofstation - DO NOT remove markup, there's an EscapeText call upstream.

        SendInSubtleRange(ChatChannel.Subtle, source, action, wrappedMessage, range);

        if (!hideLog)
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Subtle from {ToPrettyString(source):user} as {name}: {action}");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Subtle from {ToPrettyString(source):user}: {action}");
    }

    private void SendSubtleLooc(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));
        if (_adminManager.IsAdmin(player) && !_adminLoocEnabled || !_loocEnabled)
            return;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-subtle-looc-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(message)));

        SendInSubtleRange(ChatChannel.SubtleOOC, source, message, wrappedMessage, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"SOOC from {player:Player}: {message}");
    }

    /// <summary>
    /// Sends a message as a subtle
    /// </summary>
    private void SendInSubtleRange(ChatChannel channel, EntityUid source, string message, string wrappedMessage, ChatTransmitRange range)
    {
        foreach (var (session, data) in GetRecipients(source, WhisperClearRange))
        {
            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            // Post-rebase, observers can't see subtle messages unless they are admins, and subtle respects LOS for non-observers
            if (data.Observer && !CanObserverSeeSubtle(session) || data is { Observer: false, InLOS: false })
                continue;

            if (MessageRangeCheck(session, data, range) == MessageRangeCheckResult.Disallowed)
                continue;

            _chatManager.ChatMessageToOne(channel, message, wrappedMessage, source, false, session.Channel);
        }

        _replay.RecordServerMessage(new ChatMessage(channel, message, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));
    }

    /// <summary>
    /// Checks if an observer should be able to see subtle channels. Currently only allows admins to do so.
    /// </summary>
    private bool CanObserverSeeSubtle(ICommonSession session)
    {
        return _adminManager.IsAdmin(session);
    }
}
