using Content.Shared._Vulp.Weather;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Vulp.Weather.Functions;


[DataDefinition, Serializable]
public sealed partial class WeatherCleanPuddles : WeatherFunction
{
    [DataField(required: true)]
    public float CleanChance;

    [DataField(required: true)]
    public int MaxCleaned;

    [DataField(required: true)]
    public FixedPoint2 CleanAmount;

    [DataField]
    public ProtoId<ReagentPrototype> CleanReagent = "Water";

    public override void Invoke(EntityManager entMan, EntityUid map, float updateTimeSeconds)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var solutions = entMan.System<SharedSolutionContainerSystem>();
        var maps = entMan.System<SharedMapSystem>();
        var tileMan = IoCManager.Resolve<ITileDefinitionManager>();

        var query = entMan.EntityQueryEnumerator<PuddleComponent, TransformComponent>();
        var gridQuery = entMan.GetEntityQuery<MapGridComponent>();
        var cleaned = 0;
        var temperature = entMan.TryGetComponent<MapAtmosphereComponent>(map, out var atmos) ? atmos.Mixture.Temperature : 293;

        while (query.MoveNext(out var uid, out var puddle, out var xform))
        {
            if (xform.MapUid != map)
                continue;

            // Puddles shouldn't ever be off-grid, so we skip those just in case
            if (!TryGetGridOrMap(xform, out var grid, gridQuery)
                || !IsTileWeathered(grid.Value, xform.Coordinates, maps, tileMan))
                continue;

            // Chance is not multplied because the amount of cleaning is
            if (!random.Prob(CleanChance))
                continue;

            var solution = puddle.Solution;
            if (solution == null)
                continue;

            var excess = solutions.SplitSolutionWithout(solution.Value, CleanAmount * updateTimeSeconds, CleanReagent);
            solutions.TryAddReagent(solution.Value, CleanReagent, excess.Volume, temperature);

            if (++cleaned >= MaxCleaned * updateTimeSeconds)
                break;
        }
    }
}
