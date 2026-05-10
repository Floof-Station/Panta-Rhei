using System.Linq;
using System.Numerics;
using Content.Shared.Camera;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Camera;

/// <summary>
///     Handles sending rotational or translational screenshake to an entity, managing the screenshake commands
///     of every entity currently screenshaking, and setting offset/rotation when updated
/// </summary>
// DeltaV - renamed from ESScreenshakeSystem to SharedESScreenshakeSystem
public sealed class SharedESScreenshakeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Cooldowns take a string key so that multiple systems can apply their shake effects without one shake effect blocking the other.
    /// </summary>
    private readonly Dictionary<EntityUid, Dictionary<string, TimeSpan>> _shakeCooldowns = [];

    #region Internal

    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<ESScreenshakeComponent, ESGetEyeRotationEvent>(OnGetEyeRotation); // DeltaV - Moved to Client system
        // SubscribeLocalEvent<ESScreenshakeComponent, GetEyeOffsetEvent>(OnGetEyeOffset); // DeltaV - Moved to Client system
        SubscribeLocalEvent<ESScreenshakeComponent, EntityUnpausedEvent>(OnEntityUnpaused);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup); // Starlight
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO mirror might make sense to never remove individual commands and only remove the comp if theyre all > calculatedend instead.
        var shakers = EntityQueryEnumerator<EyeComponent, ESScreenshakeComponent>();
        while (shakers.MoveNext(out var ent, out var eye, out var shake))
        {
            if (shake.Commands.Count == 0)
            {
                RemCompDeferred<ESScreenshakeComponent>(ent);
                continue;
            }

            foreach (var command in shake.Commands.ToList())
            {
                // handle removing old commands
                if (_timing.CurTime < command.CalculatedEnd)
                    continue;
                shake.Commands.Remove(command);
                Dirty(ent, shake);
            }
        }
    }

    private void OnEntityUnpaused(Entity<ESScreenshakeComponent> ent, ref EntityUnpausedEvent args)
    {
        // rebuild screenshake commands but with offset times
        var newSet = new HashSet<ESScreenshakeCommand>();
        foreach (var command in ent.Comp.Commands)
        {
            var newCommand = command with
            {
                CalculatedEnd = command.CalculatedEnd + args.PausedTime,
                Start = command.Start + args.PausedTime,
            };

            newSet.Add(newCommand);
        }

        ent.Comp.Commands = newSet;
        Dirty(ent);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev) // Starlight
        => _shakeCooldowns.Clear();

    /// <summary>
    ///     Calculates when both traumas will be at least = 0 given the decay rate and start time.
    /// </summary>
    private TimeSpan CalculateEndTimeForCommand(Entity<ESScreenshakeComponent> ent, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation, TimeSpan start)
    {
        // https://www.desmos.com/calculator/optip8eucx
        var secsUntilRotationalEnd = rotation != null ? MathF.Sqrt(rotation.Trauma / rotation.DecayRate) : 0f;
        var secsUntilTranslationalEnd = translation != null ? MathF.Sqrt(translation.Trauma / translation.DecayRate) : 0f;
        var larger = secsUntilTranslationalEnd >= secsUntilRotationalEnd
            ? secsUntilTranslationalEnd
            : secsUntilRotationalEnd;

        return start + TimeSpan.FromSeconds(larger);
    }

    /// <summary>
    ///     Gets the trauma value for the current time, given the decay rate and start time.
    /// </summary>
    // DeltaV - make public
    public float CalculateTraumaValueForCurrentTime(ESScreenshakeParameters parameters, TimeSpan start)
    {
        var timeDiff = _timing.CurTime - start;

        // erm
        if (timeDiff < TimeSpan.Zero)
            return 0f;

        // trauma decays quadratically with seconds passed
        // https://www.desmos.com/calculator/optip8eucx
        var totalSecsSquared = (float) (timeDiff.TotalSeconds * timeDiff.TotalSeconds);
        return -totalSecsSquared * parameters.DecayRate + parameters.Trauma;
    }

    #endregion

    #region Public API

    // Starlight Begin
    public bool IsOnCooldown(EntityUid uid, string key)
    {
        if (!_shakeCooldowns.TryGetValue(uid, out var cooldowns))
        {
            _shakeCooldowns.Add(uid, []);
            return false;
        }
        if (!cooldowns.TryGetValue(key, out var cooldown)) return false;
        if (_timing.CurTime < cooldown) return true;
        _shakeCooldowns[uid].Remove(key); // remove from cooldowns if it shouldn't be on cooldown anymore
        return false;
    }

    public void Screenshake(EntityUid uid, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation,
        string key, float? cooldown = null)
        => Screenshake(uid, translation, rotation, key,
            cooldown is not null ? TimeSpan.FromSeconds(cooldown.Value) : null);

    public void Screenshake(EntityUid uid, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation, string key, TimeSpan? cooldown = null)
    {
        if(!_shakeCooldowns.ContainsKey(uid)) _shakeCooldowns.Add(uid, []);
        if (_shakeCooldowns[uid].TryGetValue(key, out var time))
        {
            if (_timing.CurTime < time) return;
            _shakeCooldowns[uid].Remove(key);
        }
        if(cooldown is not null)
            _shakeCooldowns[uid].Add(key, _timing.CurTime + cooldown.Value);

        Screenshake(uid, translation, rotation);
    }
    // Starlight End

    public void Screenshake(EntityUid uid, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        var comp = EnsureComp<ESScreenshakeComponent>(uid);
        var time = _timing.CurTime;
        var end = CalculateEndTimeForCommand((uid, comp), translation, rotation, time);
        var command = new ESScreenshakeCommand(translation, rotation, time, end);

        comp.Commands.Add(command);
        Dirty(uid, comp);
    }

    public void Screenshake(Filter filter, ESScreenshakeParameters? translation, ESScreenshakeParameters? rotation)
    {
        foreach (var player in filter.Recipients)
        {
            if (player.AttachedEntity is {} ent)
                Screenshake(ent, translation, rotation);
        }
    }

    #endregion
}
