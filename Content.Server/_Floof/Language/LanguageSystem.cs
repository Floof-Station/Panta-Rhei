using System.Linq;
using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Components;
using Content.Shared._Floof.Language.Events;
using Content.Shared._Floof.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Floof.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeRelay();

        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnInitLanguageSpeaker);
        SubscribeLocalEvent<LanguageSpeakerComponent, ComponentGetState>(OnGetLanguageState);
        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, DetermineEntityLanguagesEvent>(OnDetermineUniversalLanguages);

        SubscribeNetworkEvent<LanguageSetRequest>(OnClientSetLanguage);
        SubscribeNetworkEvent<ReorderLanguagesRequest>(OnClientReorderLanguages);

        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, MapInitEvent>((uid, _, _) => UpdateEntityLanguages(uid));
        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, ComponentRemove>((uid, _, _) => UpdateEntityLanguages(uid));
    }

    #region event handling

    private void OnInitLanguageSpeaker(Entity<LanguageSpeakerComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.CurrentLanguage))
            ent.Comp.CurrentLanguage = ent.Comp.SpokenLanguages.FirstOrDefault(UniversalPrototype);

        UpdateEntityLanguages(ent!);
    }

    private void OnGetLanguageState(Entity<LanguageSpeakerComponent> entity, ref ComponentGetState args)
    {
        args.State = new LanguageSpeakerComponent.State
        {
            CurrentLanguage = entity.Comp.CurrentLanguage,
            SpokenLanguages = entity.Comp.SpokenLanguages,
            UnderstoodLanguages = entity.Comp.UnderstoodLanguages
        };
    }

    private void OnDetermineUniversalLanguages(Entity<UniversalLanguageSpeakerComponent> entity, ref DetermineEntityLanguagesEvent ev)
    {
        // We only add it as a spoken language; CanUnderstand checks for ULSC itself.
        if (entity.Comp.Enabled)
            ev.SpokenLanguages.Add(UniversalPrototype);
    }

    private void OnClientSetLanguage(LanguageSetRequest message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        var language = GetLanguagePrototype(message.CurrentLanguage);
        if (language == null || !CanSpeak(uid, language.ID))
            return;

        SetLanguage(uid, language.ID);
    }

    private void OnClientReorderLanguages(ReorderLanguagesRequest msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid
            || !SpeakerQuery.TryComp(uid, out var speaker))
            return;

        // Truncate their list to just 16 languages to avoid performance overhead when ordering languages later.
        // If someone speaks more than 16 languages... well, that's on them. The language menu isn't designed to handle that many anyway.
        var order = msg.LanguageOrder.Take(16).ToList();
        speaker.PreferredOrder = order;

        UpdateEntityLanguages((uid, speaker)); // This will dirty the speaker
    }

    #endregion

    #region public api

    public override void SetLanguage(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (!CanSpeak(ent, language)
            || !SpeakerQuery.Resolve(ent, ref ent.Comp)
            || ent.Comp.CurrentLanguage == language)
            return;

        ent.Comp.CurrentLanguage = language;
        RaiseLocalEvent(ent, new LanguagesUpdateEvent(), true);
        Dirty(ent);
    }

    /// <summary>
    ///     Adds a new language to the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public override void AddLanguage(
        EntityUid uid,
        ProtoId<LanguagePrototype> language,
        bool addSpoken = true,
        bool addUnderstood = true)
    {
        DebugTools.Assert(language != UniversalPrototype, "Don't do that, add a UniversalLanguageSpeakerComponent");

        EnsureComp<LanguageKnowledgeComponent>(uid, out var knowledge);
        EnsureComp<LanguageSpeakerComponent>(uid, out var speaker);

        if (addSpoken && !knowledge.SpokenLanguages.Contains(language))
            knowledge.SpokenLanguages.Add(language);

        if (addUnderstood && !knowledge.UnderstoodLanguages.Contains(language))
            knowledge.UnderstoodLanguages.Add(language);

        UpdateEntityLanguages((uid, speaker));
    }

    /// <summary>
    ///     Removes a language from the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public override void RemoveLanguage(
        Entity<LanguageKnowledgeComponent?> ent,
        ProtoId<LanguagePrototype> language,
        bool removeSpoken = true,
        bool removeUnderstood = true)
    {
        DebugTools.Assert(language != UniversalPrototype, "Don't do that, remove the UniversalLanguageSpeakerComponent");
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (removeSpoken)
            ent.Comp.SpokenLanguages.Remove(language);

        if (removeUnderstood)
            ent.Comp.UnderstoodLanguages.Remove(language);

        // We don't ensure that the entity has a speaker comp. If it doesn't... Well, woe be the caller of this method.
        UpdateEntityLanguages(ent.Owner);
    }

    /// <summary>
    ///   Ensures the given entity has a valid language as its current language.
    ///   If not, sets it to the first entry of its SpokenLanguages list, or universal if it's empty.
    /// </summary>
    /// <returns>True if the current language was modified, false otherwise.</returns>
    public override bool EnsureValidLanguage(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!SpeakerQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.SpokenLanguages.Contains(ent.Comp.CurrentLanguage))
        {
            ent.Comp.CurrentLanguage = ent.Comp.SpokenLanguages.FirstOrDefault(UniversalPrototype);
            RaiseLocalEvent(ent, new LanguagesUpdateEvent());
            Dirty(ent);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Immediately refreshes the cached lists of spoken and understood languages for the given entity.
    /// </summary>
    public void UpdateEntityLanguages(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!SpeakerQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new DetermineEntityLanguagesEvent();
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (KnowledgeQuery.TryComp(ent, out var knowledge))
        {
            foreach (var spoken in knowledge.SpokenLanguages)
                ev.SpokenLanguages.Add(spoken);

            foreach (var understood in knowledge.UnderstoodLanguages)
                ev.UnderstoodLanguages.Add(understood);
        }

        RaiseLocalEvent(ent, ref ev);

        // We reorder the languages according to the player's preferences
        ent.Comp.SpokenLanguages = OrderByPreferences(ev.SpokenLanguages, ent.Comp.PreferredOrder);
        ent.Comp.UnderstoodLanguages = OrderByPreferences(ev.UnderstoodLanguages, ent.Comp.PreferredOrder);

        // If EnsureValidLanguage returns true, it also raises a LanguagesUpdateEvent, so we try to avoid raising it twice in that case.
        if (!EnsureValidLanguage(ent))
            RaiseLocalEvent(ent, new LanguagesUpdateEvent());

        Dirty(ent);
    }

    #endregion

    #region private api

    /// <summary>
    ///     Orders the given list of <see cref="languages"/> according to the player's <see cref="preferences"/>. <br/>
    ///     The resulting list contains the following: <br/>
    ///     - First, all languages listed in <see cref="preferences"/> that are also present in <see cref="languages"/>  <br/>
    ///     - Then all languages listed in `<see cref="languages"/> that are NOT present in <see cref="preferences"/>.
    /// </summary>
    private List<ProtoId<LanguagePrototype>> OrderByPreferences(ICollection<ProtoId<LanguagePrototype>> languages, List<ProtoId<LanguagePrototype>> preferences)
    {
        var result = new List<ProtoId<LanguagePrototype>>(languages.Count);
        foreach (var language in preferences)
        {
            if (!languages.Contains(language))
                continue;

            result.Add(language);
        }

        foreach (var language in languages)
        {
            if (result.Contains(language))
                continue;

            result.Add(language);
        }

        return result;
    }

    #endregion
}
