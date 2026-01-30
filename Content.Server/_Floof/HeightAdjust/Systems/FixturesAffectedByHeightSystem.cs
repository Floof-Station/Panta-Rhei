using Content.Server._Floof.HeightAdjust.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server._Floof.HeightAdjust.Systems;

/// <summary>
///     Adjusts the size of the humanoid's fixtures based on their height multiplier.
/// </summary>
public sealed class FixturesAffectedByHeightSystem : BaseHeightAdjustSystem<FixturesAffectedByHeightComponent>
{
    [Dependency] private readonly PhysicsSystem _physics = default!;

    protected override void OnHeightChanged(Entity<FixturesAffectedByHeightComponent> ent, ref HeightChangedEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        var mod = Math.Clamp(args.Ratio, 0.1f, 10f);
        foreach (var (key, fix) in fixtures.Fixtures)
            _physics.SetRadius(ent, key, fix, fix.Shape, fix.Shape.Radius * mod);
    }
}
