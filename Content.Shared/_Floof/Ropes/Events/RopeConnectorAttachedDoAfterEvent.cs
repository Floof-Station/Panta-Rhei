using Content.Shared._Floof.Ropes.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Ropes.Events;

[Serializable, NetSerializable]
public sealed partial class RopeConnectorAttachedDoAfterEvent : DoAfterEvent
{
    public readonly RopeSide Side;

    public RopeConnectorAttachedDoAfterEvent(EntityUid connector, RopeSide side)
    {
        Side = side;
    }

    public override DoAfterEvent Clone() => this; // This is kinda stupid
}
