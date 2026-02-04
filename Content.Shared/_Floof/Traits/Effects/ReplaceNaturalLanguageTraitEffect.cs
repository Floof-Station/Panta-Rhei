using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Components;
using Content.Shared._Floof.Language.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
///     Replaces the natural language of an entity with the specified language.
/// </summary>
public sealed partial class ReplaceNaturalLanguageTraitEffect : BaseTraitEffect
{
    /// <summary>
    ///     What language is given to the entity instead of their natural language.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LanguagePrototype> Language;

    public override void Apply(TraitEffectContext ctx)
    {
        var entMan = ctx.EntMan;
        var languageSys = entMan.System<SharedLanguageSystem>();
        var user = ctx.Player;

        if (!entMan.TryGetComponent<LanguageKnowledgeComponent>(user, out var knowledge))
            return;

        var targetLanguage = knowledge.NaturalLanguage;
        if (targetLanguage == null)
            Log.Warning($"Entity {entMan.ToPrettyString(user)} does not have a natural language, so NaturalSpeakerTrait is providing one for free");
        else
            languageSys.RemoveLanguage(user, targetLanguage.Value, true, true);

        knowledge.NaturalLanguage = Language; // So that other traits like the foreigner one notice the change
        languageSys.AddLanguage(user, Language, true, true);
    }
}
