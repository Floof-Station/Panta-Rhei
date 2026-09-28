using Content.Server._DV.Planet;
using Content.Server._DV.Station.Components;
using Content.Server._Floof.Lavaland;
using Content.Server._Lavaland.Procedural.Systems;
using Content.Shared._Floof.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._DV.Station.Systems;

public sealed class StationPlanetSpawnerSystem : EntitySystem
{
    [Dependency] private readonly PlanetSystem _planet = default!;
    [Dependency] private readonly IConfigurationManager _config = default!; // Floofstation
    [Dependency] private readonly LavalandSystem _lavaland = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPlanetSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationPlanetSpawnerComponent, ComponentShutdown>(OnShutdown);

        // Floofstation
        SubscribeLocalEvent<StationLavalandSpawnerComponent, MapInitEvent>(OnLavalandMapInit);
        SubscribeLocalEvent<StationLavalandSpawnerComponent, ComponentShutdown>(OnLavalandShutdown);
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

    // Floofstation section
    private void OnLavalandMapInit(Entity<StationLavalandSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (!_config.GetCVar(FloofCCVars.StationPlanetSpawning))
            return;

        if (_lavaland.SetupLavalandPlanet(ent.Comp.Prototype, out var map))
            ent.Comp.Planet = map?.Owner ?? EntityUid.Invalid;
    }

    private void OnLavalandShutdown(Entity<StationLavalandSpawnerComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.Planet);
    }
    // Floofstation section end
}
