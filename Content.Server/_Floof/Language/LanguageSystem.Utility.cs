using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Language;

public sealed partial class LanguageSystem
{
    /*
     *      ><(((('>          ><>       ><(((('>   ><>
     * ><(((('>              ><>                        ><(((('>
     *               >--) ) ) )*>  ><>
     */

    /// <summary>
    /// Ensures the entity has a LanguageSpeakerComponent with some defaults (speaking and understanding tau-ceti).
    /// </summary>
    public LanguageSpeakerComponent EnsureSpeaker(EntityUid entity)
    {
        if (SpeakerQuery.TryComp(entity, out var result))
            return result;

        result = new()
        {
            CurrentLanguage = FallbackLanguagePrototype,
            SpokenLanguages = [FallbackLanguagePrototype],
            UnderstoodLanguages = [FallbackLanguagePrototype],
        };
        return result;
    }

    /// <summary>
    /// Temporarily replaces the current spoken language of the entity with the provided language.
    /// Does not check if the entity can speak the provided language.<br/><br/>
    ///
    /// This method returns an IDisposable that will automatically revert the change when used in a "using..." block.
    /// </summary>
    /// <example><code>
    /// private void SaySomething(EntityUid entity) {
    ///     using var _ = _languageSystem.SubstituteEntityLanguage(entity, "Universal");
    ///     _chatSystem.TrySendInGameICMessage(entity, "Hello world from universal");
    ///     // At the end of the function (current block), the change will be automatically reverted by C#
    /// }
    /// </code></example>
    [MustDisposeResource]
    public IDisposable SubstituteEntityLanguage(Entity<LanguageSpeakerComponent?> entity, ProtoId<LanguagePrototype> language)
    {
        entity.Comp = EnsureSpeaker(entity); // This method is generally used in places where a failure is unacceptable

        var oldLanguage = entity.Comp.CurrentLanguage;
        entity.Comp.CurrentLanguage = language; // We don't use SetLanguage here to avoid raising an event & notifying the client
        return new LanguageReplacementDisposable(oldLanguage, entity!);
    }

    private struct LanguageReplacementDisposable(ProtoId<LanguagePrototype> oldLanguage, LanguageSpeakerComponent speaker) : IDisposable
    {
        public void Dispose() => speaker.CurrentLanguage = oldLanguage;
    }
}
