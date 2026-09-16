using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Ropes.Events;

[Serializable, NetSerializable]
public sealed partial class RopeConnectorDetachedDoAfterEvent : SimpleDoAfterEvent;
