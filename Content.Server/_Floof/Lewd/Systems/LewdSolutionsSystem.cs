using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Shared._Floof.Lewd;
using Content.Shared._Floof.Lewd.Components;
using Content.Shared._Floof.Util;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server._Floof.Lewd.Systems;

/// <summary>
///     Processes solutions added by the LewdOrganSystem. This is intentionally isolated as it should only perform read-only operations on the cache.
/// </summary>
public sealed class LewdSolutionsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solContainer = default!;
    [Dependency] private readonly PuddleSystem _puddles = default!;
    [Dependency] private readonly MobStateSystem _mobStates = default!;
    [Dependency] private readonly HungerSystem _hungers = default!;

    public static Ticker GlobalUpdateInterval = new(TimeSpan.FromMilliseconds(1000)); // Never update more than once every second, otherwise precision errors will bite your arm off

    public override void Update(float frameTime)
    {
        if (!GlobalUpdateInterval.TryUpdate(_timing))
            return;

        var toAdd = new List<(EntityUid, SolutionContainerManagerComponent, LewdOrganData)>();

        var query = EntityQueryEnumerator<SolutionContainerManagerComponent, LewdMobDataComponent>();
        while (query.MoveNext(out var uid, out var solMan, out var lewd))
        {
            if (!lewd.UpdateInterval.TryUpdate(_timing) || !CanProcess(uid))
                continue;

            if (lewd.UpdateInterval.Interval < GlobalUpdateInterval.Interval)
            {
                Log.Warning($"Entity {uid} has an invalid lewd organ update interval, clamping it.");
                lewd.UpdateInterval.Interval = GlobalUpdateInterval.Interval;
            }

            foreach (var (kind, lewdData) in lewd.CachedData)
            {
                if (!_solContainer.TryGetSolution((uid, solMan), lewdData.SolutionName, out var solution, errorOnMissing: false))
                {
                    // So fun fact, EnsureSolution can fail if the mob is not yet map-initialized
                    // This means that organ addition can randomly fail when done via traits.
                    Log.Warning($"Entity {uid} is missing a solution {lewdData.SolutionName}, adding it.");
                    toAdd.Add((uid, solMan, lewdData));
                    continue;
                }

                ProcessDrainage((uid, solMan, lewd), solution.Value, lewdData);
                if (CanRegenerate(uid))
                    ProcessRegeneration((uid, solMan, lewd), solution.Value, lewdData);
            }
        }

        foreach (var (uid, solMan, lewdData) in toAdd)
        {
            _solContainer.EnsureSolution(uid, lewdData.SolutionName, out _, lewdData.SolutionVolume);
        }
    }

    private void ProcessDrainage(Entity<SolutionContainerManagerComponent, LewdMobDataComponent> ent, Entity<SolutionComponent> solution, LewdOrganData data)
    {
        if (data.DrainSpeed <= 0)
            return;

        // Remove all reagents that aren't supposed to regenerate
        Solution drained;
        var dt = ent.Comp2.UpdateInterval.Interval.TotalSeconds;
        if (data.ProducedReagentPrototypes is { } regenerable)
            drained = _solContainer.SplitSolutionWithout(solution, data.DrainSpeed * dt, regenerable);
        else
            drained = _solContainer.SplitSolutionReagentsEvenly(solution, data.DrainSpeed * dt);

        if (data.SpillDrain)
            _puddles.TrySpillAt(ent, drained, out _, false);
    }

    private void ProcessRegeneration(Entity<SolutionContainerManagerComponent, LewdMobDataComponent> ent, Entity<SolutionComponent> solution, LewdOrganData data)
    {
        if (data.ProducedReagents is null || solution.Comp.Solution.AvailableVolume <= 0.01)
            return;

        // We need to produce N chemicals (usually N = 1) and make sure none of them exceed their internal caps
        var dt = ent.Comp2.UpdateInterval.Interval.TotalSeconds;
        var sol = solution.Comp.Solution;
        var added = false;
        for (var i = 0; i < data.ProducedReagents.Length; i++)
        {
            var (id, desiredQuantity) = data.ProducedReagents[i];
            var containedQuantity = sol.GetReagentQuantity(id);

            if (containedQuantity >= desiredQuantity)
                continue;

            var addQuantity = data.ProductionSpeed * dt / data.ProducedReagents.Length;
            sol.AddReagent(id, addQuantity);
            added = true;
        }

        if (added)
            _solContainer.UpdateChemicals(solution);
    }

    public bool CanProcess(EntityUid mob)
    {
        return _mobStates.IsAlive(mob);
    }

    /// <remarks>Does not check CanProcess.</remarks>
    public bool CanRegenerate(EntityUid mob)
    {
        // Must not be starving nor dehydrated in order to produce.
        return (!TryComp<HungerComponent>(mob, out var h) || _hungers.GetHungerThreshold(h) > HungerThreshold.Starving)
               && (!TryComp<ThirstComponent>(mob, out var t) || t.CurrentThirstThreshold > ThirstThreshold.Parched);
    }
}
