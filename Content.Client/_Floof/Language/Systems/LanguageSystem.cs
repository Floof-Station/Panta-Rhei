using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Components;
using Content.Shared._Floof.Language.Events;
using Content.Shared._Floof.Language.Systems;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client._Floof.Language.Systems;

public sealed class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>
    ///     Invoked when the languages of the local player entity change, for use in UI.
    /// </summary>
    public event Action? OnLanguagesChanged;

    public override void Initialize()
    {
        _playerManager.LocalPlayerAttached += OnPlayerAttached;
        SubscribeLocalEvent<LanguageSpeakerComponent, ComponentHandleState>(OnHandleState);
    }

    #region Event handling

    private void OnHandleState(Entity<LanguageSpeakerComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not LanguageSpeakerComponent.State state)
            return;

        ent.Comp.CurrentLanguage = state.CurrentLanguage;
        ent.Comp.SpokenLanguages = state.SpokenLanguages;
        ent.Comp.UnderstoodLanguages = state.UnderstoodLanguages;

        if (ent.Owner == _playerManager.LocalEntity)
            NotifyUpdate(ent);
    }

    /// <summary>
    ///     Returns the LanguageSpeakerComponent of the local player entity.
    ///     Will return null if the player does not have an entity, or if the client has not yet received the component state.
    /// </summary>
    public LanguageSpeakerComponent? GetLocalSpeaker()
    {
        return CompOrNull<LanguageSpeakerComponent>(_playerManager.LocalEntity);
    }

    public void RequestSetLanguage(ProtoId<LanguagePrototype> language)
    {
        if (GetLocalSpeaker()?.CurrentLanguage?.Equals(language) == true)
            return;

        RaiseNetworkEvent(new LanguagesSetMessage(language));
    }

    private void OnPlayerAttached(EntityUid entity)
    {
        // If the entity doesn't have a LanguageSpeakerComponent, create our own with some defaults
        // Ideally this should be handled by the server, but meh, the server already provides some defaults in case the speaker has no LSC
        if (!HasComp<LanguageSpeakerComponent>(entity))
        {
            var speaker = EnsureComp<LanguageSpeakerComponent>(entity);
            speaker.CurrentLanguage = UniversalPrototype;
            speaker.SpokenLanguages = new() { UniversalPrototype };
        }

        NotifyUpdate(entity);
    }

    private void NotifyUpdate(EntityUid localPlayer)
    {
        RaiseLocalEvent(localPlayer, new LanguagesUpdateEvent(), broadcast: true);
        OnLanguagesChanged?.Invoke();
    }

    #endregion

    public bool CanLocalPlayerUnderstand(ProtoId<LanguagePrototype> language)
    {
        if (_playerManager.LocalEntity is not { } player)
            return false;
        return CanUnderstand(player, language);
    }

    public bool CanLocalPlayerSpeak(ProtoId<LanguagePrototype> language)
    {
        if (_playerManager.LocalEntity is not { } player)
            return false;
        return CanSpeak(player, language);
    }
}
