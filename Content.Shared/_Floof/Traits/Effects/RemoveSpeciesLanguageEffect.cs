using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Traits.Effects;
/// <summary>
///     Removes all species granted languages.
/// </summary>
public sealed partial class RemoveSpeciesLanguageEffect : BaseTraitEffect
{
    public override void Apply(TraitEffectContext ctx)
    {
        var languageSys = ctx.EntMan.System<SharedLanguageSystem>();
        var user = ctx.Player;
        var languages = languageSys.GetUnderstoodLanguages(user).ShallowClone();
        foreach (var language in languages)
        {
            if (language.Id != "TauCetiBasic")
                languageSys.RemoveLanguage(user, language);
        }
    }
}
