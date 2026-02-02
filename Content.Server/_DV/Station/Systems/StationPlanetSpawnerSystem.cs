using Content.Server._DV.Planet;
using Content.Server._DV.Station.Components;
using Content.Shared._Floof.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._DV.Station.Systems;

public sealed class StationPlanetSpawnerSystem : EntitySystem
{
    [Dependency] private readonly PlanetSystem _planet = default!;
    [Dependency] private readonly IConfigurationManager _config = default!; // Floofstation

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPlanetSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationPlanetSpawnerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<StationPlanetSpawnerComponent> ent, ref MapInitEvent args)
    {
        // Floofstation
        if (!_config.GetCVar(FloofCCVars.StationPlanetSpawning))
            return;

        if (ent.Comp.GridPath is not {} path)
            return;

        ent.Comp.Map = _planet.LoadPlanet(ent.Comp.Planet, path);
    }

    private void OnShutdown(Entity<StationPlanetSpawnerComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.Map);
    }
}
