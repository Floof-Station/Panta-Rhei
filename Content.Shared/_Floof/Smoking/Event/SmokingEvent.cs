using Content.Shared.Chemistry.Components;

namespace Content.Shared._Floof.Smoking.Event;

public sealed class SmokingEvent(EntityUid smokebale, Solution solution) : EntityEventArgs
{
    public readonly EntityUid Cig = smokebale;
    public readonly Solution Solution = solution;
}
