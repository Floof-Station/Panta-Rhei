using Content.Server.Antag.Components;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Goobstation.Ghostbar.Components;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared._Floof.Language.Components;
using Content.Shared._Goobstation.Ghostbar.Events;
using Content.Shared.Abilities.Psionics;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Goobstation.Ghostbar;

public sealed class GhostBarSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly StationSpawningSystem _spawningSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    private static readonly List<String> _jobPrototypes = new()
    {
        "Passenger",
        "Bartender",
        "Botanist",
        "Chef",
        "Janitor"
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeNetworkEvent<GhostBarSpawnEvent>(SpawnPlayer);
        SubscribeLocalEvent<GhostBarPlayerComponent, MindRemovedMessage>(PlayerGhostedFromGhostbar);
    }

    private readonly ResPath _mapPath = new("Maps/Floof/Nonstation/Ghostbar/ghostbar.yml");

    private void OnRoundStart(RoundStartingEvent ev)
    {
        if (_mapLoader.TryLoadMap(_mapPath, out var map, out _, new DeserializationOptions { InitializeMaps = true }))
            _mapSystem.SetPaused(map.Value.Comp.MapId, false);
    }

    public void SpawnPlayer(GhostBarSpawnEvent msg, EntitySessionEventArgs args)
    {
        if (!EntityManager.HasComponent<GhostComponent>(args.SenderSession.AttachedEntity))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to spawn at ghost bar without being a ghost.");
            return;
        }

        var spawnPoints = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<GhostBarSpawnComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            spawnPoints.Add(EntityManager.GetComponent<TransformComponent>(ent).Coordinates);
        }

        if (spawnPoints.Count == 0)
        {
            Log.Warning("No spawn points found for ghost bar.");
            return;
        }

        var randomSpawnPoint = _random.Pick(spawnPoints);
        var randomJob = _random.Pick(_jobPrototypes);
        var profile = _ticker.GetPlayerProfile(args.SenderSession);
        var mobUid = _spawningSystem.SpawnPlayerMob(randomSpawnPoint, randomJob, profile, null);
        RaiseLocalEvent(new PlayerSpawnCompleteEvent(mobUid, args.SenderSession, randomJob, true, true, 0, EntityUid.Invalid, profile)); // we give them their characters traits

        RemComp<TemperatureComponent>(mobUid);
        RemComp<RespiratorComponent>(mobUid);
        RemComp<BarotraumaComponent>(mobUid);
        EnsureComp<MindShieldComponent>(mobUid);
        EnsureComp<AntagImmuneComponent>(mobUid); // self explanatory why we dont want players becoming antags at the ghostbar
		EnsureComp<PsionicInsulationComponent>(mobUid); // we don't want people getting mindswapped
        EnsureComp<UniversalLanguageSpeakerComponent>(mobUid); // giving universal just in case for RP purposes
        EnsureComp<GhostBarPlayerComponent>(mobUid); // give the player mob the ghostbarplayer comp so they can be tracked
        var targetMind = _mindSystem.GetMind(args.SenderSession.UserId);

        if (targetMind != null)
        {
            _mindSystem.TransferTo(targetMind.Value, mobUid, true);
        }
    }
    // Delete the players character if they choose to ghost while at the ghostbar using the GhostBarPlayerComponent
    private void PlayerGhostedFromGhostbar(Entity<GhostBarPlayerComponent> ent, ref MindRemovedMessage args)
    {
        QueueDel(ent);
    }
}
