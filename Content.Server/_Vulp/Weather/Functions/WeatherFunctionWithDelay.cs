using System.Threading;
using Content.Shared._Vulp.Weather;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Random;

namespace Content.Server._Vulp.Weather.Functions;


[ImplicitDataDefinitionForInheritors, Serializable]
public abstract partial class WeatherFunctionWithDelay : WeatherFunction
{
    [DataField]
    public MinMax DelaySeconds = new(3, 10);

    protected CancellationTokenSource? Cts;

    public override void Invoke(EntityManager entMan, EntityUid map, float updateTimeSeconds)
    {
        if (!entMan.TryGetComponent<WeatherCycleComponent>(map, out var cycle))
            return;

        var startingWeather = cycle.CurrentState?.Proto;
        if (Cts is not null && !Cts.IsCancellationRequested)
            Cts.Cancel();

        Cts = new CancellationTokenSource();
        var delay = DelaySeconds.Next(IoCManager.Resolve<IRobustRandom>());

        // FIXME: Timers are getting obsoleted, replace this with a custom timer implementation
        Robust.Shared.Timing.Timer.Spawn(TimeSpan.FromSeconds(delay),
            () =>
            {
                if (entMan.Deleted(map) || cycle.CurrentState?.Proto != startingWeather)
                    return;

                Fire(entMan, (map, cycle), updateTimeSeconds);
                Cts = null;
            });
    }

    protected abstract void Fire(EntityManager entMan,
        Entity<WeatherCycleComponent> ent,
        float updateTimeSeconds);
}
