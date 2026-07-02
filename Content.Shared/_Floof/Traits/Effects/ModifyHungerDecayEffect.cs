using Content.Shared._DV.Traits.Effects;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class ModifyHungerDecayEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public float Amount;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<HungerComponent>(ctx.Player, out var hungerComp))
        {
            Log.Error($"Trying to apply ModifyHungerDecayEffect on {ctx.Player}, but entity has no HungerComponent.");
            return;
        }
        if (!ctx.EntMan.TrySystem<HungerSystem>(out var _hunger))
        {
            Log.Error($"Trying to apply ModifyHungerDecayEffect on {ctx.Player}, but HungerSystem cannot be found.");
            return;
        }
        _hunger.SetBaseDecayRate(ctx.Player, hungerComp.BaseDecayRate + Amount, hungerComp);
    }
}
