using System.Text;
using Content.Shared._Floof.Language.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Language.Systems;

public abstract partial class SharedLanguageSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly SharedGameTicker _ticker = default!;

    /// <summary>
    ///     The language used as a fallback in cases where an entity suddenly becomes a language speaker (e.g. the usage of make-sentient)
    /// </summary>
    public static readonly ProtoId<LanguagePrototype> FallbackLanguagePrototype = "TauCetiBasic";

    /// <summary>
    ///     The language whose speakers are assumed to understand and speak every language. Should never be added directly.
    /// </summary>
    public static readonly ProtoId<LanguagePrototype> UniversalPrototype = "Universal";

    /// <summary>
    ///     A cached instance of <see cref="UniversalPrototype"/>
    /// </summary>
    public static LanguagePrototype Universal { get; private set; } = default!;

    protected EntityQuery<LanguageSpeakerComponent> SpeakerQuery = default!;
    protected EntityQuery<UniversalLanguageSpeakerComponent> UniversalQuery = default!;

    public override void Initialize()
    {
        Universal = _prototype.Index(UniversalPrototype);

        SpeakerQuery = GetEntityQuery<LanguageSpeakerComponent>();
        UniversalQuery = GetEntityQuery<UniversalLanguageSpeakerComponent>();
    }

    #region public api

    /// <summary>
    /// Checks if an entity can understand a given language. Universal speakers are assumed to understand every language.
    /// On the client side, this method is only guaranteed to work if the entity is the local player.
    /// </summary>
    public bool CanUnderstand(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> languageId) =>
        languageId == UniversalPrototype || _prototype.TryIndex(languageId, out var language) && CanUnderstand(ent, language);

    /// <inheritdoc cref="CanUnderstand(Entity&lt;Components.LanguageSpeakerComponent&gt;, ProtoId&lt;LanguagePrototype&gt;)"/>
    public bool CanUnderstand(Entity<LanguageSpeakerComponent?> ent, LanguagePrototype language)
    {
        if (language == Universal || UniversalQuery.TryComp(ent, out var uni) && uni.Enabled)
            return true;

        return SpeakerQuery.Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.UnderstoodLanguages.Contains(language.ID);
    }

    /// <summary>
    /// Checks if an entity can speak a given language.
    /// On the client side, this method is only guaranteed to work if the entity is the local player.
    /// </summary>
    public bool CanSpeak(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> languageId) =>
        _prototype.TryIndex(languageId, out var language) && CanSpeak(ent, language);

    /// <inheritdoc cref="CanSpeak(Entity&lt;Components.LanguageSpeakerComponent&gt;, ProtoId&lt;LanguagePrototype&gt;)"/>
    public bool CanSpeak(Entity<LanguageSpeakerComponent?> ent, LanguagePrototype language)
    {
        if (!SpeakerQuery.Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        return ent.Comp.SpokenLanguages.Contains(language.ID);
    }

    /// <summary>
    ///     Returns the current language of the given entity, assumes Universal if it's not a language speaker.
    /// </summary>
    public LanguagePrototype GetLanguage(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!SpeakerQuery.Resolve(ent, ref ent.Comp, logMissing: false)
            || string.IsNullOrEmpty(ent.Comp.CurrentLanguage)
            || !_prototype.TryIndex<LanguagePrototype>(ent.Comp.CurrentLanguage, out var proto)
        )
            return Universal;

        return proto;
    }

    /// <summary>
    ///     Returns the list of languages this entity can speak.
    /// </summary>
    /// <remarks>This simply returns the value of <see cref="Components.LanguageSpeakerComponent.SpokenLanguages"/>.</remarks>
    public List<ProtoId<LanguagePrototype>> GetSpokenLanguages(EntityUid uid)
    {
        return SpeakerQuery.TryComp(uid, out var component) ? component.SpokenLanguages : [];
    }

    /// <summary>
    ///     Returns the list of languages this entity can understand.
    /// </summary
    /// <remarks>This simply returns the value of <see cref="Components.LanguageSpeakerComponent.SpokenLanguages"/>.</remarks>
    public List<ProtoId<LanguagePrototype>> GetUnderstoodLanguages(EntityUid uid)
    {
        return SpeakerQuery.TryComp(uid, out var component) ? component.UnderstoodLanguages : [];
    }

    public LanguagePrototype? GetLanguagePrototype(ProtoId<LanguagePrototype> id)
    {
        _prototype.TryIndex(id, out var proto);
        return proto;
    }

    /// <summary>
    ///     Obfuscates the message using the provided language prototype.
    /// </summary>
    public string ObfuscateSpeech(string message, LanguagePrototype language)
    {
        var builder = new StringBuilder();
        language.Obfuscation.Obfuscate(builder, message, this);

        return builder.ToString();
    }

    /// <summary>
    ///     Obfuscates the message using the current spoken language of the entity. Returns the obfuscated message and the language used.
    /// </summary>
    public string ObfuscateSpeechForEntity(string message, EntityUid entity, out LanguagePrototype language)
    {
        language = GetLanguage(entity);
        return ObfuscateSpeech(message, language);
    }

    #endregion

    /// <summary>
    ///     Generates a stable pseudo-random number in the range (min, max) (inclusively) for the given seed.
    ///     One seed always corresponds to one number, however the resulting number also depends on the current round number.
    ///     This method is meant to be used in <see cref="ObfuscationMethod"/> to provide stable obfuscation.
    /// </summary>
    internal int PseudoRandomNumber(int seed, int min, int max)
    {
        // Using RobustRandom or System.Random here is a bad idea because this method can get called hundreds of times per message.
        // Each call would require us to allocate a new instance of random, which would lead to lots of unnecessary calculations.
        // Instead, we use a simple but effective algorithm derived from the C language.
        // It does not produce a truly random number, but for the purpose of obfuscating messages in an RP-based game it's more than alright.

        // Floofstation - replaced round-based obfuscation with a persistent one
        // seed = seed ^ (_ticker.RoundId * 127);
        seed = seed ^ 0x4813184;
        var random = seed * 1103515245 + 12345;
        return min + Math.Abs(random) % (max - min + 1);
    }
}
