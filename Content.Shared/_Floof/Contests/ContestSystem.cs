using Robust.Shared.Physics.Components;

namespace Content.Shared._Floof.Contests;

/// <summary>
///     This system handles contests - checking how much stronger one mob (roller) is compared to another (responder).
///     This replaces the old EE contests system which was largely unused post-rebase.
/// </summary>
public sealed class ContestSystem : EntitySystem
{
    public const float MinAdvantage = 0.01f;
    public const float MaxAdvantage = 10f; // Max advantage is clamped harder to avoid issues like "mob is insta-pickupable"

    /// <summary>
    ///     Compares the strengths of the two mobs. Currently only considers mass.
    /// </summary>
    public float StrengthContest(Entity<PhysicsComponent?> roller, Entity<PhysicsComponent?> responder)
    {
        if (!Resolve(roller, ref roller.Comp, false) || !Resolve(responder, ref responder.Comp, false))
            return 1f;

        // We don't use Mass here because it can be 0 for kinetmatic controllers and stuff
        var aStrength = roller.Comp.FixturesMass;
        var bStrength = responder.Comp.FixturesMass;

        var advantage = aStrength / bStrength;
        if (float.IsNaN(advantage))
            return 1f; // Shrug

        return Math.Clamp(advantage, MinAdvantage, MaxAdvantage);
    }
}
