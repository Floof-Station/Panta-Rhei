using Content.Shared._DV.Traits.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using System.Linq;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
/// Effect that modifies the Critical and/or Dead threshold of an entity.
/// </summary>
public sealed partial class ModifyCritDeadThresholdEffect : BaseTraitEffect
{
    /// <summary>
    /// How much to multiply the Critical threshold by.
    /// </summary>
    [DataField]
    public float CritModifier = 0f;

    /// <summary>
    /// How much to multiply the Dead threshold by.
    /// </summary>
    [DataField]
    public float DeadModifier = 0f;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<MobThresholdsComponent>(ctx.Player, out var threshDict))
            return;

        var threshy = ctx.EntMan.EntitySysManager.GetEntitySystem<MobThresholdSystem>();

        // Make some temporary values to capture the existing Crit and Dead values. 
        var newCrit = threshDict.Thresholds.FirstOrDefault(x => x.Value == MobState.Critical).Key;
        var newDead = threshDict.Thresholds.FirstOrDefault(x => x.Value == MobState.Dead).Key;

        //Make changes only if something is passed in via the trait.
        if (DeadModifier != 0){
            newDead = newDead * DeadModifier;
        }
        //Make changes only if something is passed in via the trait.
        if (CritModifier != 0){
            newCrit = newCrit * CritModifier;

            //Safeguard to make sure this value will not be too low, though this shouldn't happen.
            if (newCrit <= 5){
                newCrit = 5;
            }
        }
        //Safeguard to make sure Dead is not less than or equal to Crit.
        if (newDead <= newCrit){
            newDead = newCrit + 0.1;
        }
                
        threshy.SetMobStateThreshold(ctx.Player, newCrit, MobState.Critical, threshDict);
        threshy.SetMobStateThreshold(ctx.Player, newDead, MobState.Dead, threshDict);
    }
}
