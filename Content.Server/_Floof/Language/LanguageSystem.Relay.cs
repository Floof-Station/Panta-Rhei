using Content.Server._Floof.Language.Components;
using Content.Shared._Floof.Language.Components;
using Content.Shared._Floof.Language.Events;
using Robust.Shared.Containers;

namespace Content.Server._Floof.Language;

public sealed partial class LanguageSystem
{
    private int _languageRecursionCounter = 0;

    private void InitializeRelay()
    {
        SubscribeLocalEvent<LanguageRelayComponent, DetermineEntityLanguagesEvent>(OnRelayDetermineLanguages);
        SubscribeLocalEvent<LanguageRelaySourceComponent, LanguagesUpdateEvent>(OnRelayedUpdated);

        SubscribeLocalEvent<ContainerLanguageRelayComponent, EntInsertedIntoContainerMessage>(OnContainerRelayInsert);
        SubscribeLocalEvent<ContainerLanguageRelayComponent, EntRemovedFromContainerMessage>(OnContainerRelayRemove);
    }

    private void OnRelayDetermineLanguages(Entity<LanguageRelayComponent> ent, ref DetermineEntityLanguagesEvent args)
    {
        if (!TryComp<LanguageSpeakerComponent>(ent.Comp.RelaySource, out var sourceSpeaker))
            return;

        args.SpokenLanguages.UnionWith(sourceSpeaker.SpokenLanguages);
        args.UnderstoodLanguages.UnionWith(sourceSpeaker.UnderstoodLanguages);
    }

    private void OnRelayedUpdated(Entity<LanguageRelaySourceComponent> ent, ref LanguagesUpdateEvent args)
    {
        // God save us
        if (_languageRecursionCounter > 10)
        {
            Log.Error("Exceeded the language relay limit! This is probably caused by a relay loop!");
            foreach (var relay in ent.Comp.Relays)
                RemCompDeferred<LanguageRelayComponent>(relay);
            return;
        }

        _languageRecursionCounter++;
        try
        {
            foreach (var relay in ent.Comp.Relays)
                UpdateEntityLanguages(relay);
        }
        finally
        {
            _languageRecursionCounter--;
        }
    }

    private void OnContainerRelayInsert(Entity<ContainerLanguageRelayComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (ent.Comp.ContainerName != args.Container.ID)
            return;

        SetupLanguageRelay(ent, args.Entity);
    }

    private void OnContainerRelayRemove(Entity<ContainerLanguageRelayComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (ent.Comp.ContainerName != args.Container.ID)
            return;

        SetupLanguageRelay(ent, null);
    }

    #region public api

    public override void SetupLanguageRelay(EntityUid relayTarget, Entity<LanguageKnowledgeComponent?>? relaySourceEnt)
    {
        if (relaySourceEnt is not { } relaySource)
        {
            if (TryComp<LanguageRelayComponent>(relayTarget, out var oldRelay))
            {
                RemoveRelayTarget(relayTarget, oldRelay.RelaySource);
                RemComp(relayTarget, oldRelay);
                UpdateEntityLanguages(relayTarget);
            }
            return;
        }

        if (!TryComp<LanguageRelayComponent>(relayTarget, out var relay))
            relay = AddComp<LanguageRelayComponent>(relayTarget);
        else
            Log.Warning($"Adding language relay to {ToPrettyString(relayTarget)} which is already relayed!");

        relay.RelaySource = relaySource;
        AddRelayTarget(relayTarget, relaySource);
        EnsureSpeaker(relayTarget);
        UpdateEntityLanguages(relayTarget);
    }

    private void AddRelayTarget(EntityUid relayTarget, EntityUid relaySource)
    {
        var source = EnsureComp<LanguageRelaySourceComponent>(relaySource);
        source.Relays.Add(relayTarget);
    }

    private void RemoveRelayTarget(EntityUid relayTarget, EntityUid relaySource)
    {
        if (!TryComp<LanguageRelaySourceComponent>(relaySource, out var source))
            return;

        source.Relays.Remove(relayTarget);
        if (source.Relays.Count == 0)
            RemCompDeferred<LanguageRelaySourceComponent>(relaySource);
    }

    #endregion
}
