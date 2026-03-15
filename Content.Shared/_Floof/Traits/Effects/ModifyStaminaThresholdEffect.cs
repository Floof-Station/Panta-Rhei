using Content.Shared._DV.Traits.Effects;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._Floof.Traits.Effects;

public sealed partial class ModifyStaminaThresholdEffect : BaseTraitEffect
{
    /// <summary>
    ///     Multiplicative stamina threshold and decay bonus.
    /// </summary>
    [DataField]
    public float Multiplier = 1f;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<StaminaComponent>(ctx.Player, out var stamina))
        {
            Log.Error($"Player {ctx.EntMan.ToPrettyString(ctx.Player)} has no {nameof(StaminaComponent)}!");
            return;
        }

        stamina.CritThreshold *= Multiplier;
        stamina.Decay *= Multiplier;
        stamina.AnimationThreshold *= Multiplier;

        // There are several stamina thresholds at which the mob is slowed down, all of which need to be updated
        var newThresholds = new Dictionary<FixedPoint2, float>();
        foreach (var (threshold, speedMultiplier) in stamina.StunModifierThresholds)
            newThresholds[threshold * Multiplier] = speedMultiplier;

        stamina.StunModifierThresholds = newThresholds;
        ctx.EntMan.Dirty(ctx.Player, stamina);
    }
}
