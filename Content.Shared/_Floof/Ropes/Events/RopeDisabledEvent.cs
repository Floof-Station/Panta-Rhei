namespace Content.Shared._Floof.Ropes.Events;

/// <summary>
///     Raised on a rope after it's disabled. At this point, all of its links are in nullspace and all joints have been removed.
/// </summary>
public sealed class RopeDisabledEvent : EntityEventArgs;
