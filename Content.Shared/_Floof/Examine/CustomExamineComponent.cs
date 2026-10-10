using System.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;


namespace Content.Shared._Floof.Examine;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CustomExamineComponent : Component
{
    // This is simply so that the client can know its current custom examine messages
    // Other client will dynamically receive it over the network as needed to avoid lag
    public override bool SendOnlyToOwner => true;

    [DataField, AutoNetworkedField]
    public CustomExamineData PublicData = new(null, false);

    [DataField, AutoNetworkedField]
    public CustomExamineData SubtleData = new(null, true);
}

[DataDefinition, Serializable, NetSerializable]
public partial struct CustomExamineData : IEquatable<CustomExamineData>
{
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(60);
    public static readonly int DefaultPublicVisibilityRange = 20, DefaultSubtleVisibilityRange = 2;

    [DataField]
    public string? Content;

    [DataField]
    public int VisibilityRange;

    /// <summary>
    ///     GameTime at which the message expires. Can be zero to never expire.
    /// </summary>
    [DataField]
    public TimeSpan ExpireTime;

    /// <summary>
    ///     Whether the text should only be shown if the examiner consents to seeing ERP descriptions.
    /// </summary>
    [DataField]
    public bool RequiresConsent;

    /// <summary>
    ///     Last time the message was updated, used in the UI to prevent accidental overwrites.
    /// </summary>
    [DataField]
    public TimeSpan LastUpdate;

    // Basic constructors
    public CustomExamineData() {}

    public CustomExamineData(string? content, bool subtle)
    {
        Content = content;
        RequiresConsent = subtle;
        VisibilityRange = subtle ? DefaultSubtleVisibilityRange : DefaultPublicVisibilityRange;
    }

    // Constructors that automatically set ExpireTime and LastUpdate based on CurTime
    public CustomExamineData(string? content, bool subtle, IGameTiming timing) : this(content, subtle, timing, DefaultLifetime) {}

    public CustomExamineData(string? content, bool subtle, IGameTiming timing, TimeSpan lifetime) : this(content, subtle)
    {
        ExpireTime = timing.CurTime + lifetime;
        LastUpdate = timing.CurTime;
    }

    // God bless Rider for generating this
    public bool Equals(CustomExamineData other) =>
        Content == other.Content
        && VisibilityRange == other.VisibilityRange
        && ExpireTime.Equals(other.ExpireTime)
        && RequiresConsent == other.RequiresConsent
        && LastUpdate.Equals(other.LastUpdate);

    public override bool Equals(object? obj) => obj is CustomExamineData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Content, VisibilityRange, ExpireTime, RequiresConsent, LastUpdate);
}
