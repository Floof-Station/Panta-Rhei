using System.Linq;
using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Components;
using Content.Shared._Floof.Language.Components.Translators;
using Content.Shared._Floof.Language.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
///     When applied to a language-speaking entity, removes <see cref="BaseLanguage"/> from the lists of their languages,
///     and gives them a translator that can translate to and from that language, but in turn requires the user to speak the natural language
///     of the entity that received it.
/// </summary>
public sealed partial class ForeignerTraitEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public ProtoId<LanguagePrototype> BaseLanguage;

    /// <summary>
    ///     The translator entity prototype given to the player.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId BaseTranslator = default!;

    /// <summary>
    ///     Whether this trait prevents the entity from understanding the language.
    /// </summary>
    [DataField]
    public bool CantUnderstand = true;

    /// <summary>
    ///     Whether this trait prevents the entity from speaking the language.
    /// </summary>
    [DataField]
    public bool CantSpeak = true;

    public override void Apply(TraitEffectContext ctx)
    {
        var entMan = ctx.EntMan;
        var languageSys = entMan.System<SharedLanguageSystem>();
        var user = ctx.Player;

        if (CantUnderstand && !CantSpeak)
            Log.Warning($"Trait allows an entity to speak a language they can't understand, which is undefined behavior.");

        if (!entMan.TryGetComponent<LanguageKnowledgeComponent>(user, out var knowledge))
            return;

        var alternateLanguage = knowledge.NaturalLanguage;
        if (alternateLanguage is null)
        {
            Log.Warning($"Entity {entMan.ToPrettyString(user)} does not have a defined natural language.");
            alternateLanguage = knowledge.SpokenLanguages.Find(it => it != BaseLanguage);
        }
        else if (!knowledge.SpokenLanguages.Contains(alternateLanguage.Value))
        {
            Log.Error($"Entity {{_entMan.ToPrettyString(user)}} cannot speak its own natural language {knowledge.NaturalLanguage}");
            return;
        }

        if (string.IsNullOrEmpty(alternateLanguage))
            return;

        if (TryGiveTranslator(user, BaseTranslator, BaseLanguage, alternateLanguage.Value, out var translator, entMan))
            languageSys.RemoveLanguage(user, BaseLanguage, CantSpeak, CantUnderstand);
    }


    /// <summary>
    ///     Tries to create and give the entity a translator that translates speech between the two specified languages.
    /// </summary>
    public static bool TryGiveTranslator(
        EntityUid uid,
        string baseTranslatorPrototype,
        ProtoId<LanguagePrototype> translatorLanguage,
        ProtoId<LanguagePrototype> entityLanguage,
        out EntityUid result,
        IEntityManager entMan)
    {
        var handsSys = entMan.System<SharedHandsSystem>();
        var inventorySys = entMan.System<InventorySystem>();
        var storageSys = entMan.System<SharedStorageSystem>();

        result = EntityUid.Invalid;
        if (translatorLanguage == entityLanguage)
            return false;

        var translator = entMan.SpawnNextToOrDrop(baseTranslatorPrototype, uid);
        result = translator;

        if (!entMan.TryGetComponent<HandheldTranslatorComponent>(translator, out var handheld))
        {
            handheld = entMan.AddComponent<HandheldTranslatorComponent>(translator);
            handheld.ToggleOnInteract = true;
            handheld.SetLanguageOnInteract = true;
        }

        // Just because using list = [value] is a sandbox violation on the client side
        void Set<T>(List<T> list, T value)
        {
            list.Clear();
            list.Add(value);
        }
        Set(handheld.SpokenLanguages, translatorLanguage);
        Set(handheld.UnderstoodLanguages, translatorLanguage);
        Set(handheld.RequiredLanguages, entityLanguage);

        // Try to put it in entities hand
        if (handsSys.TryPickupAnyHand(uid, translator, false, false, false))
            return true;

        // Try to put the translator into entities bag, if it has one
        if (inventorySys.TryGetSlotEntity(uid, "back", out var bag)
            && entMan.TryGetComponent<StorageComponent>(bag, out var storage)
            && storageSys.Insert(bag.Value, translator, out _, null, storage, false, false))
            return true;

        // If all of the above has failed, just leave it at the same location as the entity
        return true;
    }
}
