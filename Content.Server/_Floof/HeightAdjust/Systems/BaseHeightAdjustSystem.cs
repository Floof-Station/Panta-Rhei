namespace Content.Server._Floof.HeightAdjust.Systems;

/// <summary>
///     Base class for systems that respond to changes in heights of humanoids.
/// </summary>
/// <typeparam name="TComp">Component type</typeparam>
public abstract class BaseHeightAdjustSystem<TComp> : EntitySystem
    where TComp : IComponent
{
    public override void Initialize()
    {
        SubscribeLocalEvent<TComp, HeightChangedEvent>(OnHeightChanged);
    }

    protected abstract void OnHeightChanged(Entity<TComp> ent, ref HeightChangedEvent args);
}
