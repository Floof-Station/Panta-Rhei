using Content.Shared._Floof.Smoking.Component;
using Content.Shared._Floof.Smoking.Event;
using Content.Shared.Fluids;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Floof.Smoking;

public sealed class MessySmokerSystem : EntitySystem
{
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize() => SubscribeLocalEvent<MessySmokerComponent, SmokingEvent>(OnSmoked);

    private void OnSmoked(Entity<MessySmokerComponent> ent, ref SmokingEvent ev)
    {
        // TODO: When MessyDrinkerSystem gets updated we need to update this as well
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.SpitChance))
            return;

        if (_netManager.IsClient)
            return;

        if (ent.Comp.SpitMessagePopup != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.SpitMessagePopup), ent, ent, PopupType.MediumCaution);

        var split = ev.Solution.SplitSolution(ent.Comp.SpitAmount);

        _puddle.TrySpillAt(ent, split, out _);
    }
}
