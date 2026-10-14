namespace Content.Shared._Euphoria.Selector.Events;

/// <summary>
///     Raised on an entity with <see cref="EntityConfigurationComponent"/> BEFORE the player makes a selection, allowing to cancel the attempt..
/// </summary>
public sealed class BeforeConfigurationSelectedEvent(
    EntityConfigurationComponent.GroupDefinition group,
    string value,
    Dictionary<string, string> currentConfig,
    EntityUid? user
) : BaseConfigurationSelectedEvent(group, value, currentConfig, user)
{
    /// <summary>
    ///     If not null, when the event is cancelled, this text will be shown as a popup.
    /// </summary>
    public string? CancelReason;

    public bool Cancelled = false;

    public void Cancel(string? reason)
    {
        CancelReason = reason;
        Cancelled = true;
    }
}
