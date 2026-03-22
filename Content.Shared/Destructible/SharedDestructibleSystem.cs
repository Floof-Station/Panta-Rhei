using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Destructible;

public abstract class SharedDestructibleSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!; // Floofstation

    /// <summary>
    /// Force entity to be destroyed and deleted.
    /// </summary>
    public bool DestroyEntity(Entity<MetaDataComponent?> owner)
    {
        var ev = new DestructionAttemptEvent();
        RaiseLocalEvent(owner, ev);
        if (ev.Cancelled)
            return false;

        var eventArgs = new DestructionEventArgs();
        RaiseLocalEvent(owner, eventArgs);

        // Floofstation - break all contacts first so that other systems like SpeedModifierContacts stop doing their things
        // This will raise EndCollideEvent on all colliding bodies, then prevent it from colliding again (which can happen due to physics sub-stepping)
        if (TryComp<PhysicsComponent>(owner, out var physics))
        {
            _physics.DestroyContacts(physics);
            _physics.SetCanCollide(owner, false, dirty: false, force: true, body: physics);
        }

        PredictedQueueDel(owner);
        return true;
    }

    /// <summary>
    /// Force entity to break.
    /// </summary>
    public void BreakEntity(EntityUid owner)
    {
        var eventArgs = new BreakageEventArgs();
        RaiseLocalEvent(owner, eventArgs);
    }
}

/// <summary>
/// Raised before an entity is about to be destroyed and deleted
/// </summary>
public sealed class DestructionAttemptEvent : CancellableEntityEventArgs
{

}

/// <summary>
/// Raised when entity is destroyed and about to be deleted.
/// </summary>
public sealed class DestructionEventArgs : EntityEventArgs
{

}

/// <summary>
/// Raised when entity was heavy damage and about to break.
/// </summary>
public sealed class BreakageEventArgs : EntityEventArgs
{

}
