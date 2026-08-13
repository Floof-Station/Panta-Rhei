using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Power.Components;
using Content.Shared._DV.Chat;
using Content.Shared._Floof.Language;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost; // Nuclear-14 - handheld radio
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed partial class RadioSystem : EntitySystem // Floofstation - made partial
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    private EntityQuery<TelecomExemptComponent> _exemptQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntityAudiblyEmotedEvent>(OnIntrinsicAudibleEmote); // DeltaV - Robots should be allowed to emote over radio.

        _exemptQuery = GetEntityQuery<TelecomExemptComponent>();
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    //Nuclear-14
    /// <summary>
    /// Gets the message frequency, if there is no such frequency, returns the standard channel frequency.
    /// </summary>
    public int GetFrequency(EntityUid source, RadioChannelPrototype channel)
    {
        if (TryComp<RadioMicrophoneComponent>(source, out var radioMicrophone))
            return radioMicrophone.Frequency;

        return channel.Frequency;
    }


    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent argsRaw)
    {
        var args = ApplyLanguageUnderstanding(argsRaw, uid); // Floofstation - languages
        if (!TryComp(uid, out ActorComponent? actor))
            return;

        var msg = args.ChatMsg;
        if (_ghost.CanGhostWarp(actor.PlayerSession, out _))
        {
            msg = new MsgChatMessage
            {
                Message = new ChatMessage(args.ChatMsg.Message)
                {
                    WrappedMessage = _chatManager.PrependFollowButtonIfAppropriate(
                        args.ChatMsg.Message.WrappedMessage,
                        args.MessageSource,
                        actor.PlayerSession.Channel),
                },
            };
        }

        _netMan.ServerSendMessage(msg, actor.PlayerSession.Channel);
    }

    // DeltaV
    private void OnIntrinsicAudibleEmote(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntityAudiblyEmotedEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid, emType: args.Type);
        }
    }
    // DeltaV - End

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(EntityUid messageSource, string message, ProtoId<RadioChannelPrototype> channel, EntityUid radioSource, bool escapeMarkup = true, int? frequency = null, EmoteType? emType = null) // Nuclear-14 - handheld radio - added frequency // DeltaV - EmoteType? added.
    {
        SendRadioMessage(messageSource, message, _prototype.Index(channel), radioSource, escapeMarkup: escapeMarkup, frequency: frequency, emType: emType); // Nuclear-14 - handheld radio - added frequency
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    /// <param name="languageOverride">Added by floofstation - allows overriding the language of the message. Defaults to the language of the radio source.</param>
    public void SendRadioMessage(EntityUid messageSource, string message, RadioChannelPrototype channel, EntityUid radioSource, bool escapeMarkup = true, LanguagePrototype? languageOverride = null, int? frequency = null, EmoteType? emType = null) // DeltaV - EmoteType? added. // N14 - frequency added
    {
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.Resolve(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        // Euphoria - most of this method was rewritten.
        // This is an active minefield. Even if you think you know what you're doing, only step in if absolutely necessary.

        // DeltaV - This change is to change up how the messages are wrapped up. Basically changing the formatting depending on the emote type.
        string wrappedMessage;
        LanguagePrototype? language = null; // Floof

        if (emType == EmoteType.Audible)
            wrappedMessage = Loc.GetString("chat-radio-message-audible-emote-wrap",
                ("color", channel.Color),
                ("channel", $"\\[{channel.LocalizedName}\\]"),
                ("name", name),
                ("message", content));
        else if (emType == EmoteType.AudiblePossessive)
            wrappedMessage = Loc.GetString("chat-radio-message-audible-possessive-emote-wrap",
                ("color", channel.Color),
                ("channel", $"\\[{channel.LocalizedName}\\]"),
                ("name", name),
                ("message", content));
        else
        {
            language = languageOverride ?? _language.GetLanguage(messageSource);
            if (!language.SpeechOverride.AllowRadio)
                return;

            // Nuclear-14 start
            string channelText;
            if (channel.ShowFrequency && frequency.HasValue)
                channelText = $"\\[{frequency}\\]";
            else
                channelText = $"\\[{channel.LocalizedName}\\]";
            // Nuclear-14 end

            // Floofstation notice: if the below gets changed, make sure to update ConstructChatMessage too
            wrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
                ("channelColor", channel.Color), // Floofstation edit: renamed to channelColor
                ("fontType", language.SpeechOverride.FontId ?? speech.FontId), // Floofstation edit
                ("fontSize", language.SpeechOverride.FontSize ?? speech.FontSize), // Floofstation edit
                ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                ("channel", channelText), // Floofstation - was this: //$"\\[{channel.LocalizedName}\\]"), was changed to the nuclear-14 channelText above
                ("name", name),
                // Floofstation. Note that we explicitly don't use channel.Color here because this is only used for the language hint.
                ("language", language.ID),
                ("textColor", ChatSystem.LanguageColorForFluent(language, new(200, 200, 200))),
                ("textFont", ChatSystem.LanguageFontForFluent(language)),
                // Floofstation section end
                ("message", content));
        }
        // DeltaV - End

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = MakeChatMessage( // Euphoria - replace with a method call
            ChatChannel.Radio,
            message,
            wrappedMessage,
            messageSource,
            null,
            speech, channel, name, language);
        var chatMsg = new MsgChatMessage { Message = chat };
        var ev = new RadioReceiveEvent(message, messageSource, channel, radioSource, chatMsg);

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();

        if (frequency == null) // Nuclear-14
            frequency = GetFrequency(messageSource, channel); // Nuclear-14

        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!HasComp<GhostComponent>(receiver) && GetFrequency(receiver, channel) != frequency) // Nuclear-14 - handheld radio - added frequency check
                continue;

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);
        }

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.RecordServerMessage(chat);
        _messages.Remove(message);
    }

    /// <inheritdoc cref="TelecomServerComponent"/>
    public bool HasActiveServer(MapId mapId, string channelId) // DeltaV - we need this
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}
