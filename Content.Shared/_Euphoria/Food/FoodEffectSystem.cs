using Content.Shared._Euphoria.FoodEffects.Components;
using Content.Shared.Nutrition;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Food.FoodEffect;

public sealed class FoodEffectSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoodEffectComponent, FullyEatenEvent>(OnFullyEaten);
    }

    private void OnFullyEaten(Entity<FoodEffectComponent> entity, ref FullyEatenEvent args)
    {
        if (TryComp<FoodEffectComponent>(entity, out var foodEffect))
        {
            var effect = foodEffect.Effect;
            var time = foodEffect.Time;
            var mode = foodEffect.Mode;
            var delay = foodEffect.Delay;

            if (!_effects.HasStatusEffect(args.User, effect))
            {
                if (mode == StatusEffectMetabolismMode.Update)
                    _effects.TryUpdateStatusEffectDuration(args.User, new EntProtoId(effect), time, delay);
            }
        }
    }

}
