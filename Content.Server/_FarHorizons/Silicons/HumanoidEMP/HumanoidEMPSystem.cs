using Content.Server.Body.Systems;
using Content.Server.Emp;
using Content.Server.Hands.Systems;
using Content.Server.Stunnable;
using Content.Shared._FarHorizons.Silicons.HumanoidEMP;
using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Server._FarHorizons.Silicons.HumanoidEMP;

public sealed partial class HumanoidEMPSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidEMPComponent, EmpPulseEvent>(OnHumanoidEMP);
        SubscribeLocalEvent<HumanoidEMPCompositeComponent, EmpPulseEvent>(OnHumanoidEMPComposite);
    }

    private void OnHumanoidEMP(Entity<HumanoidEMPComponent> ent, ref EmpPulseEvent args)
    {
        if (args.Disabled)
            return;

        ApplyEffect(ent, ent.Comp.Effect);
    }
    private void OnHumanoidEMPComposite(Entity<HumanoidEMPCompositeComponent> ent, ref EmpPulseEvent args)
    {
        if (!args.Disabled)
            ApplyEffect(ent, CompositeEffect(ent));
    }

    public HumanoidEMPEffect CompositeEffect(Entity<HumanoidEMPCompositeComponent> ent)
    {
        HumanoidEMPEffect composite = new();

        if (TryComp<HumanoidEMPComponent>(ent, out var empComp))
            composite = empComp.Effect;

        foreach (var part in _body.GetBodyChildren(ent))
            if(TryComp<HumanoidEMPCompositeElementComponent>(part.Id, out var compositeElement))
                composite += compositeElement.Effect;
        
        foreach (var organ in _body.GetBodyOrgans(ent))
            if(TryComp<HumanoidEMPCompositeElementComponent>(organ.Id, out var compositeElement))
                composite += compositeElement.Effect;

        return composite;
    }

    public void ApplyEffect(EntityUid ent, HumanoidEMPEffect effect)
    {
        _stunSystem.TryKnockdown(ent, effect.KnockdownAmount, false, true, false, true);
        _stunSystem.TryAddStunDuration(ent, effect.StunAmount);
        _damageable.TryChangeDamage(ent, effect.DamageAmount);
        _status.TryAddStatusEffectDuration(ent, effect.BlindStatusEffect, out _, effect.BlindAmount);
        _movementMod.TryAddMovementSpeedModDuration(ent, MovementModStatusSystem.FlashSlowdown, effect.SlowdownAmount, effect.WalkSpeedModifier, effect.SprintSpeedModifier);
        foreach (var hand in effect.DropItemsFrom)
            _hands.DoDrop(ent, hand);
    }
}