using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.Language.Systems;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class ModifyLanguagesTraitEffect : BaseTraitEffect
{
    /// The list of all Spoken Languages that this trait adds.
    [DataField]
    public List<string>? LanguagesSpoken { get; private set; }

    /// The list of all Understood Languages that this trait adds.
    [DataField]
    public List<string>? LanguagesUnderstood { get; private set; }

    /// The list of all Spoken Languages that this trait removes.
    [DataField]
    public List<string>? RemoveLanguagesSpoken { get; private set; }

    /// The list of all Understood Languages that this trait removes.
    [DataField]
    public List<string>? RemoveLanguagesUnderstood { get; private set; }

    public override void Apply(TraitEffectContext ctx)
    {
        var user = ctx.Player;
        var entMan = ctx.EntMan;
        var language = entMan.System<SharedLanguageSystem>();

        if (RemoveLanguagesSpoken is not null)
            foreach (var lang in RemoveLanguagesSpoken)
                language.RemoveLanguage(user, lang, true, false);

        if (RemoveLanguagesUnderstood is not null)
            foreach (var lang in RemoveLanguagesUnderstood)
                language.RemoveLanguage(user, lang, false, true);

        if (LanguagesSpoken is not null)
            foreach (var lang in LanguagesSpoken)
                language.AddLanguage(user, lang, true, false);

        if (LanguagesUnderstood is not null)
            foreach (var lang in LanguagesUnderstood)
                language.AddLanguage(user, lang, false, true);
    }
}

