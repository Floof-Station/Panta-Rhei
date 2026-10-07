using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Vore;

[Serializable, NetSerializable]
public sealed class VoreSettingsRequestEvent : EntityEventArgs {}

[Serializable, NetSerializable]
public sealed class VoreSettingsEvent : EntityEventArgs
{
    public string SettingsYaml { get; set; } = string.Empty;
}

[Serializable]
public sealed class VoreSettingsChangedEvent : EntityEventArgs
{
    public bool AllowSound { get; set; }
    public float DigestSpeed { get; set; }
    public Color StomachColor { get; set; }
}

[Serializable, NetSerializable]
public sealed class OpenVoreMenuEvent : EntityEventArgs {}