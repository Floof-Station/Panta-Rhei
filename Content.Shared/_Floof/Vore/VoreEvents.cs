using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Vore;

// settings sent from content.client to update components and to signal cvars have changed
[Serializable, NetSerializable]
public sealed class VoreSettingsChangedEvent : EntityEventArgs
{
    public Color StomachColor { get; set; }
}

// to open the menu from content.server
[Serializable, NetSerializable]
public sealed class OpenVoreMenuEvent : EntityEventArgs {}