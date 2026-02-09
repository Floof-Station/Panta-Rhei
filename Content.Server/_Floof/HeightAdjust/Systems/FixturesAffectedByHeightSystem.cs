using Content.Server._Floof.HeightAdjust.Components;
using Content.Shared._Floof.HeightAdjust;
using Robust.Shared.Physics;

namespace Content.Server._Floof.HeightAdjust.Systems;

/// <summary>
///     Adjusts the size of the humanoid's fixtures based on their height multiplier.
/// </summary>
public sealed class FixturesAffectedByHeightSystem : BaseHeightAdjustSystem<FixturesAffectedByHeightComponent>
{
    [Dependency] private readonly FixtureHelperSystem _fixtureHelper = default!;

    protected override void OnHeightChanged(Entity<FixturesAffectedByHeightComponent> ent, ref HeightChangedEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        var mod = Math.Clamp(args.Ratio, 0.1f, 10f);
        _fixtureHelper.TryAdjustFixtures((ent, fixtures), mod);
    }
}
