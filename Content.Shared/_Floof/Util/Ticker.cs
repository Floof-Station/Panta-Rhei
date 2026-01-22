using System.Globalization;
using System.Runtime.CompilerServices;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Util;

// Note: this file is licensed under the MIT license (see LICENSE_MIT.txt).

/// <summary>
///     A structure representing some event that should only fire once every few in-game ticks.
/// </summary>
/// <example><code>
///     // In component code...
///     class MyComponent : Component {
///         [DataField(required: true)] public Ticker UpdateInterval;
///     }
///     // In system code...
///     public void Update(...) {
///         if (myComponent.Ticker.TryUpdate(_gameTiming)) {
///             // A tick has occured, do something
///         }
///     }
///     // In yaml...
///     - type: entity
///       components:
///       - type: MyComponent
///         updateInterval: 180 # Update interval in seconds, parsed as a TimeSpan. You can specify initial time after a semicolon.
///         # alternative: `updateInterval: 3m`    # Update interval in minutes
///         # alternative: `updateInterval: 3m;1m` # Update interval of 3 minutes and initial time of 1 minute. Mainly for map serialization.
/// </code></example>
[DataDefinition, Serializable, NetSerializable]
public partial struct Ticker : ISelfSerialize
{
    /// <summary>
    ///     Update interval of this ticker.
    /// </summary>
    [DataField] public TimeSpan Interval;

    /// <summary>
    ///     When (in game time) was the last update.
    /// </summary>
    [DataField] public TimeSpan LastUpdate;

    private ISawmill Log => Logger.GetSawmill("ticker"); // Proper contextual logging when

    /// <summary>
    ///     Constructs a ticker that updates every [interval] seconds. If [instant] is true, LastUpdate is set to the negative of Interval,
    ///     meaning that the first call to TryUpdate will always succeed. Useful for logging and cooldowns.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ticker(TimeSpan interval, bool instant = true)
    {
        Interval = interval;
        if (instant)
            LastUpdate = -Interval;
    }

    /// <summary>
    ///     Try to update this ticker. If LastUpdate > (CurTime + LastUpdate),
    ///     this method will return true and set LastUpdate to the current tick.<br/><br/>
    ///
    ///     The main purpose of this method is to be used inside an "if" statement to check if some action should be performed.
    ///     E.g. you can introduce a "cooldown" ticker field on a component, and when checking if that component can perform its action right now,
    ///     you can call TryUpdate. If this method returns true, you may proceed with the action, otherwise you should assume it's on cooldown.
    /// </summary>
    /// <remarks>
    ///     Make sure the field this method is called on is NOT readonly, otherwise this ticker struct will always be copied on invocation,
    ///     which means that the write to LastUpdate will be lost. Rider and other IDEs should warn you of such mistakes.
    /// </remarks>
    /// <seealso cref="Ticker"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryUpdate(IGameTiming timing)
    {
        // Interval can be MaxValue, so we should avoid increasing it here
        if (timing.CurTime - Interval < LastUpdate)
            return false;

        LastUpdate = timing.CurTime;
        return true;
    }

    /// <summary>
    ///     Resets this ticker by setting its last update time to the current tick.
    ///     This means that the ticker will have to wait out its interval before being able to tick again.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(IGameTiming timing)
    {
        LastUpdate = timing.CurTime;
    }

    public void Deserialize(string value)
    {
        string? interval;
        string? lastUpdate = null;
        if (value.IndexOf(';') is var sepIndex && sepIndex >= 0)
        {
            interval = value.Substring(0, sepIndex);
            lastUpdate = value[sepIndex..];
        }
        else
            interval = value; // No last update

        if (!TimeSpanExt.TryTimeSpan(interval, out Interval))
        {
            Interval = TimeSpan.MaxValue;
            Log.Warning($"Failed to deserialize Ticker value as a TimeSpan. Malformed input: {value}.");
        }
        if (lastUpdate != null && !TimeSpanExt.TryTimeSpan(lastUpdate, out LastUpdate))
        {
            LastUpdate = TimeSpan.Zero;
            Log.Warning($"Failed to deserialize Ticker value as a TimeSpan. Malformed input: {value}.");
        }
    }

    public string Serialize()
    {
        // Don't serialize LastUpdate if it's zero
        if (LastUpdate <= TimeSpan.Zero)
            return Interval.TotalSeconds.ToString(CultureInfo.InvariantCulture);

        return Interval.TotalSeconds.ToString(CultureInfo.InvariantCulture) + ';' +
               LastUpdate.TotalSeconds.ToString(CultureInfo.InvariantCulture);
    }
}
