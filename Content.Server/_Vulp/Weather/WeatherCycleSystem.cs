using System.Linq;
using Content.Server.Weather;
using Content.Shared._Vulp.Weather;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;


namespace Content.Server._Vulp.Weather;


public sealed class WeatherCycleSystem : EntitySystem
{
    [Dependency] private readonly WeatherSystem _weather = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public override void Initialize()
    {
        _protoMan.PrototypesReloaded += ValidatePrototypes;
        ValidatePrototypes(null);
    }

    private void ValidatePrototypes(PrototypesReloadedEventArgs? args)
    {
        if (args != null && !args.WasModified<WeatherCyclePrototype>())
            return;

        // TODO: should this be an integration test?
        foreach (var proto in _protoMan.EnumeratePrototypes<WeatherCyclePrototype>())
            ValidatePrototype(proto);
    }

    /// <summary>
    ///     Validates the prototype data and sets up state IDs (WeatherCycleData.StateId). Logs errors to the console.
    /// </summary>
    public void ValidatePrototype(WeatherCyclePrototype proto)
    {
        foreach (var (id, data) in proto.Weathers)
        {
            data.StateId = id;
            if (data.Transitions is null)
                continue;

            foreach (var (refId, _) in data.Transitions)
            {
                if (proto.Weathers.ContainsKey(refId))
                    continue;

                Log.Error($"Weather prototype {proto.ID} contains an unresolved transition {refId} in its state {id}.");
            }
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<WeatherCycleComponent, MapComponent>();

        while (query.MoveNext(out var uid, out var weatherCycle, out var map))
        {
            if (_timing.CurTime < weatherCycle.NextUpdate)
                continue;

            weatherCycle.NextUpdate = _timing.CurTime + weatherCycle.UpdateInterval;
            var elapsed = weatherCycle.UpdateInterval;

            ProcessWeather((uid, weatherCycle), elapsed);
        }
    }

    public void ProcessWeather(Entity<WeatherCycleComponent> ent, TimeSpan elapsedTime)
    {
        if (!_protoMan.TryIndex(ent.Comp.Prototype, out var proto))
            return;

        if (ent.Comp.CurrentState is not {} current)
        {
            if (proto.Weathers.Count >= 1)
                SetState(ent, proto.Weathers.Values.MaxBy(it => it.Weight)!);
            return;
        }

        if (_timing.CurTime > ent.Comp.NextWeather)
        {
            AdvanceState(ent, proto);
            return;
        }

        // If the current weather has fully started, begin executing its update functions
        if (ent.Comp.CurrentWeatherEntity is {} currentWeather
            && TryComp<StatusEffectComponent>(currentWeather, out var currentWeatherComp)
            && _weather.GetWeatherPercent((currentWeather, currentWeatherComp)) >= 0.99f)
            return;

        var updateTimeSeconds = (float) elapsedTime.TotalSeconds;
        foreach (var func in current.OnUpdate)
            func.Invoke(EntityManager, ent.Owner, updateTimeSeconds);
    }

    public void AdvanceState(Entity<WeatherCycleComponent> ent, WeatherCyclePrototype cycle)
    {
        if (ent.Comp.CurrentState is not { } current || cycle.Weathers.Count == 0)
            return;

        var newId = current.Transitions is not null
            ? current.Transitions.ToList().WeightedRandom(_random, it => it.Value).Key
            : cycle.Weathers.ToList().WeightedRandom(_random, it => it.Value.Weight).Key;

        if (!cycle.Weathers.TryGetValue(newId, out var newState))
        {
            Log.Error($"Encountered invalid weather state reference: {newId} in weather cycle {cycle.ID}.");
            newState = cycle.Weathers.Values.MaxBy(it => it.Weight)!;
        }

        ent.Comp.Prototype = cycle.ID; // Just in case adminbus changed it
        SetState(ent, newState);
    }

    /// <summary>
    ///     Transitions the weather on the map associated with this weather cycle into the specified state.
    ///     Does NOT clear the old weather!
    /// </summary>
    public void SetState(Entity<WeatherCycleComponent> ent, WeatherCycleData state)
    {
        var oldState = ent.Comp.CurrentState;
        var isRepeatedTraversal = state == oldState;

        var duration = TimeSpan.FromSeconds(state.DurationSeconds.Next(_random) * ent.Comp.TimeScale);
        ent.Comp.NextWeather = _timing.CurTime + duration;
        ent.Comp.CurrentState = state;

        var proto = state.Proto == null ? null : _protoMan.TryIndex(state.Proto, out var weather) ? weather : null;
        if (Transform(ent).MapID is { } map && proto is not null)
        {
            _weather.TryAddWeather(map, proto, out var weatherEnt, duration);
        }

        // Run any transition functions on the new state
        foreach (var func in state.OnTransition)
        {
            if (!isRepeatedTraversal || func.InvokeOnRepeatedTraversal)
                func.Invoke(EntityManager, ent.Owner, 1f);
        }
    }

    /// <summary>
    ///     Should be invoked whenever the weather gets replaced externally on a map, such as when invoking weather:set.
    /// </summary>
    public void HandleExternalWeatherChange(MapId map, EntProtoId? newWeather, TimeSpan duration)
    {
        if (!_maps.TryGetMap(map, out var mapUid))
            return;

        // Vulpstation - adjust the weather cycle
        if (!TryComp<WeatherCycleComponent>(mapUid, out var cycle) ||
            !_protoMan.TryIndex(cycle.Prototype, out var cycleProto))
            return;

        var newState = cycleProto.Weathers.Values
            .Where(it => it.Proto == newWeather)
            .OrderByDescending(it => it.Weight)
            .FirstOrDefault();

        if (newState == null)
        {
            cycle.CurrentState = new(); // Alas, let's hope and pray for the best.
            Log.Warning($"External weather change on map {map}. Weather is managed according to the {cycleProto.ID} weather cycle, but no state with the prototype {newWeather} was found in it.");
        }
        else
        {
            SetState((mapUid.Value, cycle), newState);
            Log.Info($"Weather cycle on map {map} has been changed due to an admin command. Transitioned to state {newState} for {duration}.");
        }

        // The logic here is as follows: invoking the weather command with a specific weather can delay the cycle by however long the admemes want,
        // But clearing the weather should always resume the weather cycle at some point (unless a specific duration is given)
        cycle.NextWeather = _timing.CurTime + duration;
    }
}
