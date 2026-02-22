using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Leash;

[Serializable, NetSerializable]
public sealed partial class LeashAttachDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class LeashDetachDoAfterEvent : SimpleDoAfterEvent
{
}
