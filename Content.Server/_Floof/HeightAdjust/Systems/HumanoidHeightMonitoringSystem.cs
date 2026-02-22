using Content.Server._Floof.HeightAdjust.Components;
using Content.Shared._DV.Humanoid;
using Content.Shared.Humanoid;

namespace Content.Server._Floof.HeightAdjust.Systems;

public sealed class HumanoidHeightMonitoringSystem : EntitySystem
{
    private EntityQuery<HumanoidAppearanceComponent> _humanoidQuery;

    public override void Initialize()
    {
        _humanoidQuery = GetEntityQuery<HumanoidAppearanceComponent>();

        SubscribeLocalEvent<HumanoidHeightMonitoringComponent, AppearanceLoadedEvent>(OnAppearenceLoaded);
    }

    private void OnAppearenceLoaded(Entity<HumanoidHeightMonitoringComponent> ent, ref AppearanceLoadedEvent args)
    {
        if (!_humanoidQuery.TryComp(ent, out var humanoid))
            return;

        var oldScale = ent.Comp.LastHeight;
        var newScale = humanoid.Height;
        // Ignore minor changes
        if (MathHelper.CloseTo(oldScale, newScale, 0.005))
            return;

        if (oldScale <= 0.001)
            oldScale = 1;
        if (newScale <= 0.001)
            newScale = 1;

        var ratio = newScale / oldScale;
        ent.Comp.LastHeight = newScale;

        RaiseLocalEvent(ent, new HeightChangedEvent(oldScale, newScale, ratio));
    }
}
