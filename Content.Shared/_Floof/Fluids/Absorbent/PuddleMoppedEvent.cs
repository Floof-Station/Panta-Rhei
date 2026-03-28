namespace Content.Shared._Floof.Fluids.Absorbent;

/// <summary>
///     Raised from AbsorbentSystem.Mop() when a user succesfully tries to mop a puddle.
///     Not raised on any successive mopping attempts made e.g. using TryPuddleInteract.
/// </summary>
public sealed class PuddleMoppedEvent : EntityEventArgs
{
    public required EntityUid User, Used, Target;
}
