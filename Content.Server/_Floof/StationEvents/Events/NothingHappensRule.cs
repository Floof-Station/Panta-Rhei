using Content.Server._Floof.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Floof.StationEvents.Events;

public sealed class NothingHappensRule : StationEventSystem<NothingHappensRuleComponent>
{
    // Need to end it to avoid clogging the history and admin command completions
    protected override void Started(EntityUid uid, NothingHappensRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args) =>
        ForceEndSelf(uid, gameRule);
}
