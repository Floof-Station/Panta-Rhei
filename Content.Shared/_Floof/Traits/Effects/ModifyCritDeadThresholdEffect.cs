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
    //[Dependency] private readonly MobThresholdSystem _thresholds = default!;
    
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

        //The new Dictionary we'll be playing with.
        //SortedDictionary<FixedPoint2, MobState> newDict = new SortedDictionary<FixedPoint2, MobState>();
        //newDict.Add(0, MobState.Alive);

        // Make some temporary values to capture the existing Crit and Dead values. 
        var newCrit = threshDict.Thresholds.FirstOrDefault(x => x.Value == MobState.Critical).Key;
        var newDead = threshDict.Thresholds.FirstOrDefault(x => x.Value == MobState.Dead).Key;

        if (newDead != null){
            //Make changes only if something is passed in via the trait.
            if (DeadModifier != 0){
                newDead = newDead * DeadModifier;
            }
            
            //Grab the existing Crit value. There shouldn't be a case in which we send in a Dict that doesn't have Crit, but let's account for that anyway.
            if (newCrit != null){
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
                
                //newDict.Add(newCrit, MobState.Critical);
                //newDict.Add(newDead, MobState.Dead);
                //_thresholds.SetMobStateThreshold(ctx.Player, newCrit, MobState.Critical, threshDict);
                //_thresholds.SetMobStateThreshold(ctx.Player, newDead, MobState.Dead, threshDict);
                threshy.SetMobStateThreshold(ctx.Player, newCrit, MobState.Critical, threshDict);
                threshy.SetMobStateThreshold(ctx.Player, newDead, MobState.Dead, threshDict);
            }
            else { //I don't think there's a scenario in which a player will not have a Critical threshold, but just in case...
                //There is no reason the Dead value should ever be 0 or lower, but just in case...
                if (newDead <= 0) {
                    newDead = 5;
                }
                //newDict.Add(newDead, MobState.Dead);
                //_thresholds.SetMobStateThreshold(ctx.Player, newDead, MobState.Dead, threshDict);
                threshy.SetMobStateThreshold(ctx.Player, newDead, MobState.Dead, threshDict);
            }
        }
        else { //If the Dead threshold doesn't exist, something went horribly wrong. This should never happen, but just in case...
            //newDict.Add(1, MobState.Dead); //A value greater than 0.
            //_thresholds.SetMobStateThreshold(ctx.Player, 1, MobState.Dead, threshDict); //A value greater than 0.
            threshy.SetMobStateThreshold(ctx.Player, 1, MobState.Dead, threshDict); //A value greater than 0.
        }
    }
}
