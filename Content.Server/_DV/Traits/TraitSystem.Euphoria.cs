using System.Linq;
using Content.Shared._DV.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.Traits;

// Euph-specific additions
public sealed partial class TraitSystem
{
        /**
     * Trait validation is order-dependant due to counting point limits. Try to put traits in an order that makes sense.
     * This means add all point-negative traits first, then all the positive traits, so that traits can never get rejected for points
     * if the final total points are under the limit.
     * Finally, evaluate more expensive traits before less expensive traits, so that the final validated set is as close to the point
     * limit as possible.
     */

    /// <summary>
    /// Orders traits to prevent errors in order-dependant trait validation. When iterating accross the returned enumerable, a trait with
    /// zero or negative cost will never be encountered after a trait with positive cost. Among traits with positive cost, lowest-cost
    /// traits will be encountered last.
    /// </summary>
    /// <param name="traitPrototypes">An enumerable of trait prototypes.</param>
    /// <returns>An ordered enumerable of trait prototypes ready to run through trait validation.</returns>
    public static IOrderedEnumerable<TraitPrototype> OrderTraitPrototypesForValidation(IEnumerable<TraitPrototype> traitPrototypes)
    {
        return traitPrototypes.OrderBy((trait) => trait.Cost > 0).ThenByDescending((trait) => trait.Cost);
    }

    /// <summary>
    /// Orders traits ready for to apply. Higher priority traits are ordered first.
    /// </summary>
    /// <param name="traitPrototypes">An enumerable of trait prototypes.</param>
    /// <returns>An ordered enumerable of trait prototypes ready to apply.</returns>
    private static IOrderedEnumerable<TraitPrototype> OrderTraitPrototypesForApplication(IEnumerable<TraitPrototype> traitPrototypes)
    {
        return traitPrototypes.OrderByDescending((a) => a.Priority);
    }

    /// <summary>
    /// Resolve each trait ID in selectedTraits to its corresponding trait, where that trait exists. Invalid trait IDs are rejected.
    /// </summary>
    /// <param name="traitIds">A collection of arbitrary trait ids</param>
    /// <returns>An enumerable containing trait prototypes</returns>
    public IEnumerable<TraitPrototype> ResolveTraitPrototypes(IReadOnlyCollection<ProtoId<TraitPrototype>> traitIds)
    {
        var resolvedTraits = new List<TraitPrototype>(traitIds.Count);

        foreach (var traitId in traitIds)
        {
            if (!_prototype.TryIndex(traitId, out var trait))
            {
                Log.Warning($"Unknown trait ID in player preferences: {traitId}");
                continue;
            }

            resolvedTraits.Add(trait);
        }

        return resolvedTraits;
    }
}
