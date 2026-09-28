using Content.Shared._DV.Traits.Effects;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class ModifyThirstDecayEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public float Amount;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<ThirstComponent>(ctx.Player, out var thirstComp))
        {
            Log.Error($"Trying to apply ModifyHungerDecayEffect on {ctx.Player}, but entity has no HungerComponent.");
            return;
        }
        if (!ctx.EntMan.TrySystem<ThirstSystem>(out var _thirst))
        {
            Log.Error($"Trying to apply ModifyHungerDecayEffect on {ctx.Player}, but HungerSystem cannot be found.");
            return;
        }
        _thirst.SetBaseDecayRate(ctx.Player, thirstComp.BaseDecayRate + Amount, thirstComp);
    }
}
