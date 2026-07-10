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

public sealed class NothingHappensRule : StationEventSystem<NothingHappensRuleComponent>
{
    // Need to end it to avoid clogging the history and admin command completions
    protected override void Started(EntityUid uid, NothingHappensRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args) =>
        ForceEndSelf(uid, gameRule);
}
