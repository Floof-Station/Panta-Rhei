using System.Diagnostics.CodeAnalysis;
using Content.Server._Floof.Language;
using Content.Server.Hands.Systems;
using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Floofstation-specific stuff
/// </summary>
public sealed partial class ChatSystem
{
    [Dependency] private readonly LanguageSystem _languages = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

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

    /// <summary>
    /// Checks if the entity can currently speak its language. Returns the language it's speaking.
    /// In certain cases this can cause a popup to appear over <paramref name="entity"/> unless <paramref name="silent"/> is true.
    /// </summary>
    private bool CanSpeakLanguage(EntityUid entity, out LanguagePrototype language, bool silent = false, bool ignoreActionBlocker = false)
    {
        language = _languages.GetLanguage(entity);
        if (ignoreActionBlocker)
            return true;

        if (language.SpeechOverride.RequireSpeech && !_actionBlocker.CanSpeak(entity))
        {
            if (!silent)
                _popups.PopupEntity(Loc.GetString("chat-manager-language-requires-speech"), entity, entity, PopupType.Medium);
            return false;
        }

        // TODO harcoded 2 is bad but not like bad bad
        if (language.SpeechOverride.RequireHands &&
            (!_actionBlocker.CanComplexInteract(entity) || _hands.CountFreeHands(entity) < 2))
        {
            if (!silent)
                _popups.PopupEntity(Loc.GetString("chat-manager-language-requires-hands"), entity, entity, PopupType.Medium);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Wraps a message into a wrapper string for display in UI. This particular method is meant to only be used in SendEntitySpeak.
    /// </summary>
    private (MessageWrapData normalMessage, MessageWrapData obfuscatedMessage) WrapEntitySpeech(
        SpeechVerbPrototype speechProto,
        string speakerName,
        string message,
        LanguagePrototype language)
    {
        ExtractSpeechInfo(speechProto, language, new(200, 200, 200), out var fontColor, out var fontId, out var fontSize, out var verbs);
        var locId = speechProto.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message";

        var wrappedMessage = Loc.GetString(locId,
            ("entityName", speakerName),
            ("verb", Loc.GetString(_random.Pick(verbs))),
            ("fontType", fontId),
            ("fontSize", fontSize),
            ("textColor", fontColor), // Notice it's $textColor instead of $color
            ("message", FormattedMessage.EscapeText(message)),
            ("language", LanguageNameForFluent(language)));

        var obfuscated = _languages.ObfuscateSpeech(message, language);
        var wrappedObfuscatedMessage = Loc.GetString(locId,
            ("entityName", speakerName),
            ("verb", Loc.GetString(_random.Pick(verbs))),
            ("fontType", fontId),
            ("fontSize", fontSize),
            ("textColor", fontColor), // Notice it's $textColor instead of $color
            ("message", FormattedMessage.EscapeText(obfuscated)),
            ("language", LanguageNameForFluent(language)));

        return (new(message, wrappedMessage, language), new(obfuscated, wrappedObfuscatedMessage, language));
    }

    private MessageWrapData WrapEntityWhisper(string name,
        string message,
        bool canClearlyHear,
        bool identityKnown,
        bool languageKnown,
        LanguagePrototype? language = null)
    {
        language ??= SharedLanguageSystem.Universal;
        message = FormattedMessage.EscapeText(message);

        var locId = identityKnown ? "chat-manager-entity-whisper-wrap-message" : "chat-manager-entity-whisper-unknown-wrap-message";
        // If the language is unknown, obfuscate it
        var finalMsg = languageKnown ? message : _languages.ObfuscateSpeech(message, language);
        // If the listener doesn't have an LOS, further obfuscate it
        if (!canClearlyHear)
            finalMsg = ObfuscateMessageReadability(finalMsg, 0.2f);

        var fontColor = LanguageColorForFluent(language, new(0xA5, 0xA5, 0xA5));
        var wrappedMessage = Loc.GetString(locId,
            ("entityName", name),
            ("message", finalMsg),
            ("textColor", fontColor), // Notice it's $textColor instead of $color
            ("language", LanguageNameForFluent(language)));

        return new(finalMsg, wrappedMessage, language);
    }

    /// <summary>
    ///     Returns the visible name of the language as a Fluent string for use in localization.
    ///     It will be the literal string "null" if the language is not supposed to have a language hint.
    /// </summary>
    public static string LanguageNameForFluent(LanguagePrototype? language)
    {
        if (language is not { IsVisibleLanguage: true })
            return "null"; // For use in Fluent case matching
        return language.ChatName;
    }

    public static void ExtractSpeechInfo(SpeechVerbPrototype speechProto,
        LanguagePrototype language,
        Color defaultColor,
        out string fontColor,
        out string fontId,
        out int fontSize,
        out List<LocId> verbs)
    {
        fontColor = LanguageColorForFluent(language, defaultColor);
        fontId = language.SpeechOverride.FontId ?? speechProto.FontId;
        fontSize = language.SpeechOverride.FontSize ?? speechProto.FontSize;
        verbs = language.SpeechOverride.SpeechVerbOverrides ?? speechProto.SpeechVerbStrings;
    }

    /// <summary>
    ///     Returns the font color language as a Fluent string for use in localization.
    /// </summary>
    public static string LanguageColorForFluent(LanguagePrototype language, Color defaultColor) =>
        (language.SpeechOverride.Color ?? defaultColor).ToHex();

    public static string LanguageFontForFluent(LanguagePrototype? language) =>
        language?.SpeechOverride.FontId ?? "null";
}

public struct MessageWrapData
{
    /// <summary>
    /// The original message being transmitted
    /// </summary>
    public string Original;
    // /// <summary>
    // /// The version of this message obfuscated via language obfuscation
    // /// </summary>
    public string Wrapped;

    public LanguagePrototype Language;

    public static readonly MessageWrapData Empty = new(string.Empty, string.Empty, SharedLanguageSystem.Universal);

    public MessageWrapData(string original, string wrapped, LanguagePrototype language)
    {
        Original = original;
        Wrapped = wrapped;
        Language = language;
    }

    /// <summary>
    /// Constructs a message wrap that is spoken in universal, for use in LOOC and other channels that require no language obfuscation.
    /// </summary>
    public MessageWrapData(string original, string wrapped)
    {
        Original = original;
        Wrapped = wrapped;
        Language = SharedLanguageSystem.Universal;
    }
}
