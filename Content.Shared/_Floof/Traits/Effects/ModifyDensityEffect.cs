using Content.Shared._DV.Traits.Effects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
/// Effect that modifies the density of an entity.
/// </summary>
public sealed partial class ModifyDensityEffect : BaseTraitEffect
{
    /// <summary>
    /// The factor to multiply density by.
    /// </summary>
    [DataField(required: true)]
    public float Factor = 1f;

    public override void Apply(TraitEffectContext ctx)
    {
        //Abort if attempting to apply to entity with no fixtures
        if (!ctx.EntMan.TryGetComponent<FixturesComponent>(ctx.Player, out var fixcmp))
            return;

        //Adjust each fixture on the entity
        foreach (var (id, fix) in fixcmp.Fixtures)
        {
            var result = fix.Density * Factor;
            ctx.EntMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().SetDensity(ctx.Player, id, fix, result, update: false, fixcmp);
        }

        //Update all changes
        ctx.EntMan.EntitySysManager.GetEntitySystem<FixtureSystem>().FixtureUpdate(ctx.Player, true, true, fixcmp);
    }
}
