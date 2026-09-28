using System.Diagnostics.CodeAnalysis;
using Content.Shared.Maps;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;


namespace Content.Shared._Vulp.Weather;



[ImplicitDataDefinitionForInheritors, Serializable]
public abstract partial class WeatherFunction
{
    /// <summary>
    ///     Whether this function should be invoked when the same node is reached twice or more in a row.
    ///     Aka when the clear weather transitions into clear weather, or the like.
    /// </summary>
    public virtual bool InvokeOnRepeatedTraversal => true;

    public abstract void Invoke(EntityManager entMan, EntityUid map, float updateTimeSeconds);


    /// <summary>
    ///     Checks if the grid tile on the specified coordinates is affected by weather.
    /// </summary>
    protected static bool IsTileWeathered(Entity<MapGridComponent> grid, EntityCoordinates coords, SharedMapSystem maps, ITileDefinitionManager tileMan)
    {
        var tile = maps.GetTileRef(grid, coords);
        if (tile.Tile.IsEmpty)
            return true;

        // TODO: might want to check WeatherSystem.CanWeatherAffect
        // We can't use that function because right now all of our planet maps have ImplicitRoofComponent which makes every tile rooved
        var tileDef = (ContentTileDefinition) tileMan[tile.Tile.TypeId];
        return tileDef.Weather;
    }

    /// <summary>
    ///     Returns the grid or map the entity is on. Does NOT work if the entity itself is a grid.
    /// </summary>
    protected static bool TryGetGridOrMap(TransformComponent xform, [NotNullWhen(true)] out Entity<MapGridComponent>? grid, EntityQuery<MapGridComponent> query)
    {
        if (query.TryGetComponent(xform.GridUid, out var gridGrid))
        {
            grid = (xform.GridUid.Value, gridGrid);
            return true;
        }

        // Planet grid?
        if (query.TryGetComponent(xform.MapUid, out var mapGrid))
        {
            grid = (xform.MapUid.Value, mapGrid);
            return true;
        }

        grid = null;
        return false;
    }
}
