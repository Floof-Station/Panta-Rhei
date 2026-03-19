using Content.Server.Chat.Systems;
using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
/// Floofstation-specific stuff
/// </summary>
public sealed partial class RadioSystem
{
    [Dependency] private readonly SharedLanguageSystem _language = default!;

    /// <summary>
    /// I hate
    /// This method constructs a ChatMessage and also figures out its intended language and does language obfuscation
    /// </summary>
    private ChatMessage MakeChatMessage(ChatChannel channel,
        string message,
        string wrappedMessage,
        EntityUid sender,
        int? senderKey,
        SpeechVerbPrototype speech,
        RadioChannelPrototype radioChannel,
        string senderName,
        LanguagePrototype? languageOverride)
    {
        var msg = new ChatMessage(channel, message, wrappedMessage, GetNetEntity(sender), senderKey);
        var language = (languageOverride ?? _language.GetLanguage(sender));
        msg.Language = language;

        if (language != SharedLanguageSystem.Universal)
        {
            msg.ObfuscatedMessage = _language.ObfuscateSpeech(message, _language.GetLanguagePrototype(language)!);
            // Copy-pasted from SendRadioMessage, make sure to update accordingly
            msg.ObfuscatedWrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
                ("color", radioChannel.Color),
                ("fontType", speech.FontId),
                ("fontSize", speech.FontSize),
                ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                ("channel", $"\\[{radioChannel.LocalizedName}\\]"),
                ("name", senderName),
                ("language", ChatSystem.LanguageNameForFluent(language)), // Floofstation
                ("languageColor", language.SpeechOverride.Color ?? radioChannel.Color), // Floofstation
                ("languageFont", language.SpeechOverride.FontId ?? speech.FontId), // Floofstation
                ("message", msg.ObfuscatedMessage)); // We shouldn't need to escape this because language obfuscation doesn't (shouldn't) preserve markup tags
        }

        return msg;
    }

    /// <summary>
    /// Helper method that transforms the message according to whether or not the listener should be able to understand it.
    /// This assumes that the original chat has the ObfuscatedMessage, Language, etc. optional fields set (see MakeChatMessage).
    /// The purpose of this shitcode is to avoid changing upstream radio system and the dozens of cancerous tendrils that depend on it.
    /// </summary>
    public ChatMessage ApplyLanguageUnderstanding(ChatMessage original, EntityUid listener)
    {
        if (original.Language == default || original is not { ObfuscatedMessage: not null, ObfuscatedWrappedMessage: not null })
            return original;

        if (!Exists(listener) || _language.CanUnderstand(listener, original.Language))
            return original;

        // Yes, this is terrible. I hate myself. I hate chat code. I hate radio code. I hate hate. I can't.
        // Every second I spend working on this code... wait, we've already been there.
        // Sigh. If this ever breaks just rewrite the whole thing from scratch.
        var msg = original with { Message = original.ObfuscatedMessage, WrappedMessage = original.ObfuscatedWrappedMessage };
        return msg;
    }

    /// <inheritdoc cref="ApplyLanguageUnderstanding(ChatMessage, EntityUid)"/>
    /// <remarks>
    /// Listeners of RadioReceiveEvent often simply relay ChatMsg to another method, while ignoring all other fields of RadioReceiveEvent
    /// This method simply returns a copy of the event with the ChatMsg field modified to avoid changing upstream code.
    /// </remarks>
    public RadioReceiveEvent ApplyLanguageUnderstanding(RadioReceiveEvent original, EntityUid listener)
    {
        var msgCopy = ApplyLanguageUnderstanding(original.ChatMsg.Message, listener);
        if (msgCopy == original.ChatMsg.Message)
            return original;

        var netMsgCopy = new MsgChatMessage { Message = msgCopy };
        return original with { ChatMsg = netMsgCopy };
    }

    /// <inheritdoc cref="ApplyLanguageUnderstanding(RadioReceiveEvent, EntityUid)"/>
    /// ...
    /// By the way, if you update some piece of code using this method and something horribly breaks,
    /// Then make sure you aren't writing over a `ref` parameter. Make a new local variable and write into it.
    /// Upstream event handlers utilizing this method have had their `args` parameter renamed to `argsRaw`
    public RadioReceiveEvent ApplyLanguageUnderstanding(RadioReceiveEvent original, EntityUid? listener) =>
        listener == null ? original : ApplyLanguageUnderstanding(original, listener.Value);
}
