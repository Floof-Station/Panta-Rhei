using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Language;

[Prototype]
public sealed partial class LanguagePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LanguagePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    ///     Whether this language will display its name in chat behind a player's name.
    /// </summary>
    [DataField]
    public bool IsVisibleLanguage { get; set; }

    /// <summary>
    ///     Obfuscation method used by this language. By default, uses <see cref="ObfuscationMethod.Default"/>
    /// </summary>
    [DataField]
    public ObfuscationMethod Obfuscation = ObfuscationMethod.Default;

    /// <summary>
    ///     Speech overrides used for messages sent in this language.
    /// </summary>
    [DataField("speech")]
    public SpeechOverrideInfo SpeechOverride = new();

    /// <summary>
    ///     Icon to display in the chat in place of LanguageIconTag.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public SpriteSpecifier? Icon = new SpriteSpecifier.Rsi(new("/Textures/_Floof/Interface/Misc/language_icons.rsi"), "default.png");

    #region utility
    /// <summary>
    ///     The in-world name of this language, localized.
    /// </summary>
    public string Name => Loc.GetString($"language-{ID}-name");

    /// <summary>
    ///     The in-world chat abbreviation of this language, localized.
    /// </summary>
    [Obsolete("Currently returns Name. Abbreviations are obsolete.")]
    public string ChatName => Name;// Loc.GetString($"chat-language-{ID}-name");

    /// <summary>
    ///     The in-world description of this language, localized.
    /// </summary>
    public string Description => Loc.GetString($"language-{ID}-description");
    #endregion utility
}

[DataDefinition]
public sealed partial class SpeechOverrideInfo
{
    /// <summary>
    ///     Color which text in this language will be blended with.
    ///     Alpha blending is used, which means the alpha component of the color controls the intensity of this color.
    /// </summary>
    [DataField]
    public Color? Color = null;

    [DataField]
    public string? FontId;

    [DataField]
    public int? FontSize;

    [DataField]
    public bool AllowRadio = true, AllowWriting = true;

    /// <summary>
    ///     If true, the message will be relayed to the Empathy Chat and
    ///     anyone with that language will also hear Empathy Chat.
    ///     This is mostly only use for "Marish" but... fuckit modularity :p
    /// </summary>
    /// TODO FLOOFSTATION REMOVE THIS
    [Obsolete("DO NOT USE. This is terrible code. Make a special handler for this language.")]
    [DataField]
    public bool EmpathySpeech = false;

    /// <summary>
    ///     If false, the entity can use this language even when it's unable to speak (i.e. muffled or muted),
    ///     and accents are not applied to messages in this language.
    /// </summary>
    [DataField]
    public bool RequireSpeech = true;

    // Floof section start
    /// <summary>
    ///     If true, requires the entity to have usable hands and be able to interact (not be cuffed, etc).
    /// </summary>
    [DataField]
    public bool RequireHands = false;

    /// <summary>
    ///     If true, the listener must have a line of sight on the speaker to hear the message.
    /// </summary>
    [DataField]
    public bool RequireLOS = false;
    // Floof section end

    /// <summary>
    ///     If not null, all messages in this language will be forced to be spoken in this chat type.
    /// </summary>
    [DataField]
    public InGameICChatType? ChatTypeOverride;

    /// <summary>
    ///     Speech verb overrides. If not provided, the default ones for the entity are used.
    /// </summary>
    [DataField]
    public List<LocId>? SpeechVerbOverrides;

    /// <summary>
    ///     Overrides for different kinds chat message wraps. If not provided, the default ones are used.
    /// </summary>
    /// <remarks>
    ///     Currently, only local chat and whispers support this. Radio and emotes are unaffected.
    ///     This is horrible.
    /// </remarks>
    [DataField]
    public Dictionary<InGameICChatType, LocId> MessageWrapOverrides = new();
}
