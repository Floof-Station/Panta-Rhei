using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Map;
using Content.Server.Spawners.Components;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed class AntagStationSpawnRuleSystem : GameRuleSystem<AntagStationSpawnRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagStationSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, AntagStationSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        // we have to select this here because AntagSelectLocationEvent is raised twice because MakeAntag is called twice
        // once when a ghost role spawner is created and once when someone takes the ghost role

        //Attempt to get the coordinates
        ChooseRandomPlayerSpawnCoords(out var coords);
        comp.Coords = coords;

        if (coords is null)
        {
            if (TryFindRandomTile(out _, out _, out _, out var randomCoords))
                comp.Coords = randomCoords;
        }
    }

    private void OnSelectLocation(Entity<AntagStationSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords != null)
            args.Coordinates.Add(_transform.ToMapCoordinates(ent.Comp.Coords.Value));
    }

    private void ChooseRandomPlayerSpawnCoords(out EntityCoordinates? coords)
    {
        coords = null;

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();
        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.LateJoin)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count <= 0)
            return;

        coords = _random.Pick(possiblePositions);
    }
}
