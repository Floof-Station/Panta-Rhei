using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Map;
using Content.Server.Spawners.Components;
using Robust.Shared.Random;

namespace Content.Server.Antag;

/// <remarks>
/// This system could have been made to slim down the pool of possible spawnpoints
/// based on the player's set preferences, but there's going to be an antag rework
/// upstream (Wizden). Normally I'm a staunch advocate for doing things the best one
/// can the first time, but this system may need to be changed regardless.
/// That nice extra stuff might as well be implemented then, I think.
/// </remarks>
public sealed class AntagStationSpawnRuleSystem : GameRuleSystem<AntagStationSpawnRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
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

        //Attempt to get the coordinates of a random latejoin spawnpoint and assign it to the component
        ChooseRandomLateJoinSpawnPointCoords(out var coords);
        comp.Coords = coords;

        //If we couldn't get any spawnpoint coords, try and get any random tile on a station.
        if (coords is null)
            if (TryFindRandomTile(out _, out _, out _, out var randomCoords))
                comp.Coords = randomCoords;
    }

    private void OnSelectLocation(Entity<AntagStationSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords != null)
            args.Coordinates.Add(_transform.ToMapCoordinates(ent.Comp.Coords.Value));
    }

    /// <summary>
    /// Choose random coordinates from all of the existing latejoin spawnpoints.
    /// </summary>
    private void ChooseRandomLateJoinSpawnPointCoords(out EntityCoordinates? coords)
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
