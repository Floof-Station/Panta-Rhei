using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Lewd;

// Can't access() this because in order to support immutable semantics I'd have to write 300 lines of shitcode, see HumanoidAppearance
[Serializable, NetSerializable, DataDefinition]
public sealed partial class LewdOrganData
{
    /// <summary>
    ///     Organ kind. Used in organ mapping.
    /// </summary>
    [DataField(required: true)]
    public LewdOrganKind OrganKind = LewdOrganKind.None;

    /// <summary>
    ///     Solution related to this organ.
    /// </summary>
    [DataField(required: true)]
    public string SolutionName = string.Empty;

    /// <summary>
    ///     Total volume of the solution. Make sure to account for the amount the solution can store, if at all.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 SolutionVolume = 0;

    /// <summary>
    ///     What reagents this organ produces, and what are their caps (each reagent has an induvidual cap). The production speed is dictated by ProductionSpeed, and is divided across all reagents.
    ///     Call LewdOrganSystem.UpdateData whenever this changes.
    /// </summary>
    [DataField]
    public ReagentQuantity[]? ProducedReagents = null;

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
    public ProtoId<ReagentPrototype>[]? ProducedReagentPrototypes = null;

    #endregion

    public LewdOrganData(LewdOrganKind organKind, FixedPoint2 solutionVolume, ReagentQuantity[]? producedReagents, FixedPoint2 productionSpeed, FixedPoint2 drainSpeed, bool spillDrain)
    {
        OrganKind = organKind;
        SolutionVolume = solutionVolume;
        ProducedReagents = producedReagents;
        ProducedReagentPrototypes = ProducedReagents?.Select(it => (ProtoId<ReagentPrototype>) it.Reagent.Prototype).ToArray();
        ProductionSpeed = productionSpeed;
        DrainSpeed = drainSpeed;
        SpillDrain = spillDrain;
    }
}
