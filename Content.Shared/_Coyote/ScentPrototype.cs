using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("scent")]
public sealed partial class ScentPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc />
    /// <summary>
    /// NOTE TO DAN: the field in the yaml is "parent", but this is "Parents" to match the interface
    /// </summary>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ScentPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Examine text for this scent
    /// </summary>
    [DataField("msgsExamine")]
    public List<string> ScentsExamine = new();

    /// <summary>
    /// Direct scent messages, no range
    /// </summary>
    [DataField("msgsDirect")]
    public List<string> ScentsDirect = new();

    #region Close Range Scent
    /// <summary>
    /// The close range scent messages
    /// </summary>
    [DataField("msgsClose")]
    public List<string> ScentsClose = new();

    /// <summary>
    /// Range at which close scents can be smelled
    /// </summary>
    [DataField("closeRange")]
    public float CloseRange = 2.0f;
    #endregion

    #region Far Range Scent
    /// <summary>
    /// The far range scent messages
    /// </summary>
    [DataField("msgsFar")]
    public List<string> ScentsFar = new();

    /// <summary>
    /// Range at which far scents can be smelled
    /// </summary>
    [DataField("farRange")]
    public float FarRange = 7.0f;
    #endregion

    /// <summary>>
    /// The min time between a certain player smelling this scent again
    /// In seconds
    /// </summary>
    [DataField("minCooldownSeconds")]
    public int MinCooldown = 120;

    /// <summary>
    /// The max time between a certain player smelling this scent again
    /// In seconds
    /// </summary>
    [DataField("maxCooldownSeconds")]
    public int MaxCooldown = 300;

    /// <summary>
    /// percent chance of being detected per interval
    /// </summary>
    [DataField("detectionPercent")]
    public int DetectionPercent;

    /// <summary>
    /// Whether or not this scent is considered "bad"
    /// </summary>
    [DataField("isStinky")]
    public bool Stinky = false;

    /// <summary>
    /// Whether or not this scent is considered "lewd"
    /// </summary>
    [DataField("isLewd")]
    public bool Lewd = false;

    /// <summary>
    /// Direct only?
    /// </summary>
    [DataField("directOnly")]
    public bool DirectOnly = false;

    /// <summary>
    /// Requires line of sight to smell
    /// </summary>
    [DataField("requireLoS")]
    public bool RequireLoS = false;

    /// <summary>
    /// Priority multiplier for this scent
    /// Higher means more likely to be smelled first
    /// </summary>
    [DataField("priorityMultiplier")]
    public float PriorityMultiplier = 1.0f;

    /// <summary>
    /// Component(s) on ScentEmitter (me) that blocks this scent from being detected
    /// </summary>
    [DataField("preventingComponents")]
    public ComponentRegistry PreventingComponents = new();

    /// <summary>
    /// Component(s) on SmellDetector (you) that blocks this scent from being detected
    /// </summary>
    [DataField("blockingComponents")]
    public ComponentRegistry BlockingComponents = new();
}
