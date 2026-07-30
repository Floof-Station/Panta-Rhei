using Content.Shared.Drunk;

namespace Content.Shared._Impstation.Traits.Assorted;
    // Euphoria - The HeavyweightDrunkSystem fill did not exist, so I created it, but it is still using Imps code
public sealed class HeavyweightDrunk : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeavyweightDrunkComponent, SharedDrunkSystem.DrunkEvent>(OnHeavyweightDrinking); //imp
    }

    private void OnHeavyweightDrinking(Entity<HeavyweightDrunkComponent> entity, ref SharedDrunkSystem.DrunkEvent args)
    {
        args.Duration *= entity.Comp.BoozeStrengthMultiplier;
    }
}




