using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Lewd;

[Serializable, NetSerializable, DataDefinition]
public partial class LewdOrganData
{
    /// <summary>
    ///     Organ kind. Used in organ mapping.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind OrganKind;

    /// <summary>
    ///     Solution related to this organ.
    /// </summary>
    [DataField(required: true)]
    public string SolutionName;

    /// <summary>
    ///     Total volume of the solution. Make sure to account for the amount the solution can store, if at all.
    /// </summary>
    public FixedPoint2 SolutionVolume;

    /// <summary>
    ///     What reagents this organ produces, and what are their caps (each reagent has an induvidual cap). The production speed is dictated by ProductionSpeed, and is divided across all reagents.
    ///     Call LewdOrganSystem.UpdateData whenever this changes.
    /// </summary>
    [DataField]
    public ReagentQuantity[]? ProducedReagents;

    [DataField]
    public FixedPoint2 ProductionSpeed = 0.05f; // 1 unit every 20 seconds

    /// <summary>
    ///     If this field is non-zero, any reagent not included in ProducedReagents will be drained at this exact speed.
    /// </summary>
    [DataField]
    public FixedPoint2 DrainSpeed = 0.05f; // Same as production

    /// <summary>
    ///     If true, the organs spills reagents when draining them. Might cause consent issues.
    /// </summary>
    [DataField]
    public bool SpillDrain = false;

    #region Caching

    /// <summary>
    ///     <see cref="ProducedReagents"/> but containing only reagent names, for use in solutions.
    /// </summary>
    [DataField(serverOnly: true)]
    public ProtoId<ReagentPrototype>[]? ProducedReagentPrototypes;

    #endregion
}
