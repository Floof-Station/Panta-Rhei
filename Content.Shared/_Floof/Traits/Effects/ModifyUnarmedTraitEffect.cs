using Content.Shared._DV.Traits.Effects;
using Content.Shared.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Traits.Effects;
/// <summary>
///     Used for traits that modify unarmed damage on MeleeWeaponComponent.
/// </summary>
public sealed partial class ModifyUnarmedTraitEffect : BaseTraitEffect
{
    // <summary>
    //     The sound played on hitting targets.
    // </summary>
    [DataField]
    public SoundSpecifier? SoundHit;

    // <summary>
    //     The animation to play on hit, for both light and power attacks.
    // </summary>
    [DataField]
    public EntProtoId? Animation;

    // <summary>
    //     Whether to set the power attack animation to be the same as the light attack.
    // </summary>
    [DataField]
    public bool HeavyAnimationFromLight = true;

    // <summary>
    //     The damage values of unarmed damage.
    // </summary>
    [DataField]
    public DamageSpecifier? Damage;

    // <summary>
    //     Additional damage added to the existing damage.
    // </summary>
    [DataField]
    public DamageSpecifier? FlatDamageIncrease;

    // <summary>
    //     What to multiply the melee weapon range by.
    // </summary>
    [DataField]
    public float? RangeModifier;

    // <summary>
    //     What to multiply the attack rate by.
    // </summary>
    [DataField]
    public float? AttackRateModifier;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<MeleeWeaponComponent>(ctx.Player, out var melee))
            return;

        if (SoundHit != null)
            melee.HitSound = SoundHit;

        if (Animation != null)
            melee.Animation = Animation.Value;

        if (HeavyAnimationFromLight)
            melee.WideAnimation = melee.Animation;

        if (Damage != null)
            melee.Damage = Damage;

        if (FlatDamageIncrease != null)
            melee.Damage += FlatDamageIncrease;

        if (RangeModifier != null)
            melee.Range *= RangeModifier.Value;

        if (AttackRateModifier != null)
            melee.AttackRate *= AttackRateModifier.Value;

        ctx.EntMan.Dirty(ctx.Player, melee);
    }
}
