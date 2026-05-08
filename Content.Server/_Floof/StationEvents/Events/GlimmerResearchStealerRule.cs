using Content.Server._Floof.StationEvents.Components;
using Content.Server.Research.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Robust.Shared.Random;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.StationEvents.Events;

internal sealed class GlimmerResearchStealerRule : StationEventSystem<GlimmerResearchStealerRuleComponent>
{
    [Dependency] private readonly SharedResearchSystem _research = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RadioSystem _radioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    protected override void Started(EntityUid uid, GlimmerResearchStealerRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        //Populate list with all servers that have technology unlocked
        List<EntityUid> serverList = new();
        var query = EntityQueryEnumerator<ResearchServerComponent, TechnologyDatabaseComponent>();
        while (query.MoveNext(out var server, out _, out var servercomp))
        {
            if (servercomp.UnlockedTechnologies.Count != 0)
                serverList.Add(server);
        }

        //If no servers have technology unlocked, end the event
        if (serverList.Count == 0)
            return;

        //Declare the server with the most technology the target of the event
        var target = serverList[0];
        int targetCount;
        int servCount;
        foreach (var serv in serverList)
        {
            targetCount = EnsureComp<TechnologyDatabaseComponent>(target).UnlockedTechnologies.Count;
            servCount = EnsureComp<TechnologyDatabaseComponent>(serv).UnlockedTechnologies.Count;
            if (targetCount < servCount)
                target = serv;
        }

        //Remove a random amount of technologies from the target server
        var ev = new ResearchStolenEvent(uid, target, new());
        var database = EnsureComp<TechnologyDatabaseComponent>(target);
        var count = _random.Next(comp.MinToSteal, comp.MaxToSteal + 1);
        for (var i = 0; i < count; i++)
        {
            if (database.UnlockedTechnologies.Count == 0)
                break;

            var toRemove = _random.Pick(database.UnlockedTechnologies);
            if (_research.TryRemoveTechnology((target, database), toRemove))
                ev.Techs.Add(toRemove);
        }

        //Play sound and produce effect when event occurs
        _popup.PopupEntity(Loc.GetString("glimmer-tech-steal", ("count", count)), target);
        _audio.PlayPvs(comp.GlimmerStealSound, target);

        //Announce the event on the epistemics radio channel
        var message = Loc.GetString("glimmer-tech-steal-message");
        var channel = _prototypeManager.Index<RadioChannelPrototype>(comp.AnnouncementChannel);
        _radioSystem.SendRadioMessage(target, message, channel, target, escapeMarkup: false);
    }
}
