using Content.Shared._DV.Traits.Effects;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;

namespace Content.Shared._Floof.Traits.Effects;

public partial class ChangePassiveHealingEffect : BaseTraitEffect
{
    [DataField(required: true)] public DamageSpecifier AddedDamage = default!;

    [DataField] public float? NewDamageCap = null;
    
    // I forgor the signature
    public override void Apply(TraitEffectContext args)
    {
        if (!args.EntMan.TryGetComponent<PassiveDamageComponent>(args.Player, out var passiveDamage))
            return;

        passiveDamage.Damage += AddedDamage;

        if (NewDamageCap is not null)
          damage.DamageCap = Math.Max(damage.DamageCap, NewDamageCap.Value);
    }
}
