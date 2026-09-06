using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._Floof.HeightAdjust;

public sealed class FixtureHelperSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

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
        var uniqueShapes = new HashSet<IPhysShape>();
        foreach (var (key, fix) in ent.Comp.Fixtures)
        {
            if (fix.Shape is not PhysShapeCircle circle || circle.Radius <= 0.01f)
                continue;

            // Sanity check
            if (!uniqueShapes.Add(fix.Shape))
            {
                Log.Warning($"Entity {ToPrettyString(ent)} has two or more fixtures that reference the same IPhysShape object. This can cause errors.");
                continue;
            }

            // Can we avoid the costly SetRadius in batch fixture updates like this?
            // Setting fixture.Radius and calling FixtureUpdate would be an option, but it's internal API
            _physics.SetRadius(ent, key, fix, fix.Shape, fix.Shape.Radius * multiplier, ent);
            count++;
        }

        return count;
    }

    public bool TryCopyShape(IPhysShape valueShape, [NotNullWhen(true)] out IPhysShape? shapeCopy)
    {
        shapeCopy = valueShape switch
        {
            PhysShapeCircle circle => new PhysShapeCircle(circle.Radius, circle.Position),
            // There is no way to set the fields of PhysShapeAABB from code, I'm not even kidding
            PhysShapeAabb aab => _serialization.CreateCopy(aab, notNullableOverride: true),
            // Same here
            PolygonShape polygon => _serialization.CreateCopy(polygon, notNullableOverride: true),
            _ => null,
        };

        if (shapeCopy == null)
        {
            Log.Error($"Cannot copy shape of type {valueShape.GetType()}");
            return false;
        }

        return true;
    }
}
