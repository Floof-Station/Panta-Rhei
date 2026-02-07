using System.Linq;
using Content.Shared._DV.Traits.Conditions;

namespace Content.Shared._Floof.Traits.Condition;

using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

/// <summary>
/// A variant of IsSpeciesCondition that checks if the player belongs to one of the listed species.
/// Use Invert = true to check if the player is NOT the species.
/// </summary>
public sealed partial class OneOfSpeciesCondition : BaseTraitCondition
{
    /// <summary>
    /// The species IDs to check for.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SpeciesPrototype>> Species;

    protected override bool EvaluateImplementation(TraitConditionContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.SpeciesId))
            return false;

        return Species.Contains(ctx.SpeciesId);
    }

    public override string GetTooltip(IPrototypeManager proto, ILocalizationManager loc)
    {
        var speciesNames = Species
            .Select(it => proto.TryIndex(it, out var speciesProto) ? speciesProto.Name : it.Id)
            .ToList();

        var namesJoined = string.Join(", or ", speciesNames); // Hardcoded "or", cry about it
        return Invert
            ? loc.GetString("trait-condition-species-not", ("species", namesJoined))
            : loc.GetString("trait-condition-species-is", ("species", namesJoined));
    }
}
