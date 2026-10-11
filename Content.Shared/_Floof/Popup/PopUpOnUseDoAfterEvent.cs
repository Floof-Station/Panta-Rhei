using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Popup;

[Serializable, NetSerializable]
public sealed partial class PopUpOnUseDoAfterEvent : SimpleDoAfterEvent
{
}
