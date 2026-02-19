using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Floof.HeightAdjust;

public class FixtureHelperSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    ///     Multiplies the radii of all fixtures of the given entity by the specified value.
    /// </summary>
    /// <returns>How many fixtures were affected. If 0, this method had no effect.</returns>
    public int TryAdjustFixtures(Entity<FixturesComponent?> ent, float multiplier)
    {
        if (multiplier <= 0)
            throw new ArgumentException(nameof(multiplier));

        if (MathHelper.CloseTo(multiplier, 1f) || !Resolve(ent, ref ent.Comp))
            return 0;

        var count = 0;
        foreach (var (key, fix) in ent.Comp.Fixtures)
        {
            if (fix.Shape is not PhysShapeCircle circle || circle.Radius <= 0.01f)
                continue;

            // Can we avoid the costly SetRadius in batch fixture updates like this?
            // Setting fixture.Radius and calling FixtureUpdate would be an option, but it's internal API
            _physics.SetRadius(ent, key, fix, fix.Shape, fix.Shape.Radius * multiplier, ent);
            count++;
        }

        return count;
    }
}
