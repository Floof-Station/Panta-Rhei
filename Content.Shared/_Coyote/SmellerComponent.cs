namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This is attached to entities that can smell scents.
/// Requires a client, otherwise whats the point?
/// </summary>
[RegisterComponent]
public sealed partial class SmellerComponent : Component
{
    /// <summary>
    /// The dict of scents we smelled, and the next time we can smell them again.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, TimeSpan> SmelledScentsCooldowns = new();

    /// <summary>
    /// List of scents that we should be smelling, but spaced out
    /// mainly to prevent spam.
    /// </summary>
    [ViewVariables]
    public List<SmellTicket> PendingSmells = new();

    /// <summary>
    /// The min time between smell processing ticks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("smellProcessingTickIntervalRange")]
    public Vector2i SmellProcessingTickIntervalRange = new(10, 20);

    /// <summary>
    /// The next time we can process smells.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextSmellProcessingTime = TimeSpan.Zero;

    /// <summary>
    /// The next time we can detect smells.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextSmellDetectionTime = TimeSpan.Zero;

    /// <summary>
    /// The interval between smell detection attempts.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("smellDetectionInterval")]
    public TimeSpan SmellDetectionInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The next time we can update pending smells.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextPendingSmellUpdateTime = TimeSpan.Zero;

    /// <summary>
    /// The interval between pending smell updates.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public TimeSpan PendingSmellUpdateInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Set of GUIDs that we are ignoring smells from.
    /// Used to temporarily ignore smells from certain sources.
    /// </summary>
    [ViewVariables]
    public HashSet<string> IgnoredScentInstanceIds = new();
}
