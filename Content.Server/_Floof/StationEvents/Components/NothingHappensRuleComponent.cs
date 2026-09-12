namespace Content.Server._Floof.StationEvents.Components;

/// <summary>
///     Marker for the "nothing happens" event.
///     Immediately ends itself to avoid clogging the gamerule history.
/// </summary>
[RegisterComponent]
public sealed partial class NothingHappensRuleComponent : Component { }
