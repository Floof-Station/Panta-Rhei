// [[file:../../../Org/_Floof/CivilAntagonists/AntagStationSpawnRule.org::System that gets the desirable station and spawn coordinates for the antagonist][System that gets the desirable station and spawn coordinates for the antagonist]]
using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Shuttles.Components;
using Robust.Shared.Map;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
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
        HandlePlayerSpawningButNotStupid(out var coords);
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

    private void HandlePlayerSpawningButNotStupid(out EntityCoordinates? coords)
    {
        coords = null;

        TryGetArrivals(out var arrivals);

        if (!TryComp(arrivals, out TransformComponent? arrivalsXform))
            return;

        var mapId = arrivalsXform.MapID;

        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();
        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.LateJoin || xform.MapID != mapId)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count <= 0)
            return;

        coords = _random.Pick(possiblePositions);
    }

    private PlayerSpawningEvent GetSpawnEventFromHandlePlayerSpawning()
    {
        var playerSpawnEv = new PlayerSpawningEvent(default, default, default);

        _ent.System<ArrivalsSystem>().HandlePlayerSpawning(playerSpawnEv);

        return playerSpawnEv;
    }

    /// <summary>
    /// Try to get the coordinates of an entity that has <see cref="ArrivalsSourceComponent"/>,
    /// or at least any tile on a station.
    /// </summary>
    private bool TryGetAnArrivalsOrRandomTileCoords(out EntityCoordinates coords)
    {
        if (TryGetArrivals(out var arrivals))
        {
            coords = Transform(arrivals).Coordinates;
            return true;
        }

        if (TryFindRandomTile(out _, out _, out _, out coords))
            return true;

        return false;
    }

    //The method below is a carbon copy of the one of the same name in ArrivalsSystem.cs,
    //which is marked as private. I don't want to make changes to upstream files, so
    //hooray for code copying. TODO: Sort that out upstream or find a better way,
    //because this is disgusting.

    /// <summary>
    /// Try to get an entity that has <see cref="ArrivalsSourceComponent"/>
    /// </summary>
    private bool TryGetArrivals(out EntityUid uid)
    {
        var arrivalsQuery = EntityQueryEnumerator<ArrivalsSourceComponent>();

        while (arrivalsQuery.MoveNext(out uid, out _))
        {
            return true;
        }

        return false;
    }
}
// System that gets the desirable station and spawn coordinates for the antagonist ends here
