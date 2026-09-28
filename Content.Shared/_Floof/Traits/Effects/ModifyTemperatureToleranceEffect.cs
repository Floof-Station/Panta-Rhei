using Content.Shared._DV.Traits.Effects;
using Content.Shared.Temperature.Components;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
/// Effect that modifies the temperature tolerances of an entity.
/// </summary>
public sealed partial class ModifyTemperatureToleranceEffect : BaseTraitEffect
{
    /// <summary>
    /// How much to adjust heat tolerance by, in Kelvin/Celcius.
    /// </summary>
    [DataField]
    public float HeatToleranceModifier = 0f;

    /// <summary>
    /// How much to adjust heat tolerance by, in Kelvin/Celcius.
    /// </summary>
    [DataField]
    public float ColdToleranceModifier = 0f;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<TemperatureDamageComponent>(ctx.Player, out var tempComp))
            return;

        tempComp.HeatDamageThreshold += HeatToleranceModifier;
        tempComp.ColdDamageThreshold += ColdToleranceModifier;
    }
}
