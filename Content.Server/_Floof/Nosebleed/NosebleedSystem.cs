using Content.Server._Floof.Nosebleed.Component;
using Content.Server.Body.Systems;
using Content.Shared._Floof.Util;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Floof.Nosebleed;

public sealed class NosebleedSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public static Ticker GlobalUpdateInterval = new(TimeSpan.FromMilliseconds(1000)); // stop checking everything every tick

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NosebleedComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, NosebleedComponent comp, ComponentStartup args)
    {
        ScheduleNextNosebleed(uid, comp);
    }

    public override void Update(float frameTime)
    {
        if (!GlobalUpdateInterval.TryUpdate(_timing))
            return;

        var query = EntityQueryEnumerator<NosebleedComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = new Entity<Component.NosebleedComponent>(uid, comp);

            if (!ent.Comp.NextNosebleedInterval.TryUpdate(_timing))
                continue;

            CauseNosebleed(ent);
        }
    }

    private void ScheduleNextNosebleed(EntityUid ent, NosebleedComponent comp)
    {
        var delay = _random.Next(TimeSpan.FromSeconds(comp.MinimumDelay), TimeSpan.FromSeconds(comp.MaximumDelay));
        comp.NextNosebleedInterval.Interval = delay;
    }

    private void CauseNosebleed(Entity<NosebleedComponent> ent)
    {
        ScheduleNextNosebleed(ent.Owner, ent);

        if (!TryComp<MobStateComponent>(ent.Owner, out var mobState))
            return;

        // are they not alive? it would be funny if we let it happen on the dead...
        if (!_mobState.IsAlive(ent.Owner, mobState))
            return;

        _popup.PopupEntity(Loc.GetString("nosebleed-message"), ent.Owner, ent.Owner, PopupType.MediumCaution);

        // bleed on the floor time (the poor janitors im sorry)
        _bloodstream.TryModifyBleedAmount(ent.Owner, ent.Comp.BleedAmount);
    }
}
