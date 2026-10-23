using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Vore;

// settings sent from content.client to update components
[Serializable, NetSerializable]
public sealed class VoreSettingsChangedEvent : EntityEventArgs
{
    public bool AllowSound { get; set; }
    public float DigestSpeed { get; set; }
    public Color StomachColor { get; set; }
}

// to open the menu from content.server
[Serializable, NetSerializable]
public sealed class OpenVoreMenuEvent : EntityEventArgs {}