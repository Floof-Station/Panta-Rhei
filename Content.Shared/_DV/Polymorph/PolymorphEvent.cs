namespace Content.Shared._DV.Polymorph;

/// <summary>
/// Raised directed on an entity before polymorphing it.
/// </summary>
[ByRefEvent]
public record struct BeforePolymorphedEvent();


/// <summary>
///     Euph - raised BEFORE trying to polymorph it, unlike BeforePolymoprhed.
/// </summary>
public sealed class PolymorphAttemptEvent : CancellableEntityEventArgs
{
    public string? CancelReason;
}
