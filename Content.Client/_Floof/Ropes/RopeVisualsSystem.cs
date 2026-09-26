using Robust.Client.Graphics;

namespace Content.Client._Floof.Ropes;

public sealed class RopeVisualsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new RopeVisualsOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<RopeVisualsOverlay>();
    }
}
