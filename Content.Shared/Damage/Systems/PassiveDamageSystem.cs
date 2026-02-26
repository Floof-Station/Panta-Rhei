using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class PassiveDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassiveDamageComponent, MapInitEvent>(OnPendingMapInitA);
        SubscribeLocalEvent<TraitRegenerationComponent, MapInitEvent>(OnPendingMapInitB);
        SubscribeLocalEvent<TraitDegenerationComponent, MapInitEvent>(OnPendingMapInitC);
        SubscribeLocalEvent<DragonHeartComponent, MapInitEvent>(OnPendingMapInitD);
    }

    private void OnPendingMapInitA(EntityUid uid, PassiveDamageComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }
    
    private void OnPendingMapInitB(EntityUid uid, TraitRegenerationComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }

    private void OnPendingMapInitC(EntityUid uid, TraitDegenerationComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }
    
    private void OnPendingMapInitD(EntityUid uid, DragonHeartComponent component, MapInitEvent args)
    {
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }
    // Every tick, attempt to damage entities
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        // Go through every entity with the component
        var queryA = EntityQueryEnumerator<PassiveDamageComponent, DamageableComponent, MobStateComponent>();
        var queryB = EntityQueryEnumerator<TraitRegenerationComponent, DamageableComponent, MobStateComponent>();
        var queryC = EntityQueryEnumerator<TraitDegenerationComponent, DamageableComponent, MobStateComponent>();
        var queryD = EntityQueryEnumerator<DragonHeartComponent, DamageableComponent, MobStateComponent>();
        while (queryA.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            // Make sure they're up for a damage tick
            if (comp.NextDamage > curTime)
                continue;

            if (comp.DamageCap != 0 && damage.TotalDamage >= comp.DamageCap)
                continue;

            // Set the next time they can take damage
            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            // Damage them
            foreach (var allowedState in comp.AllowedStates)
            {
                if(allowedState == mobState.CurrentState)
                    _damageable.ChangeDamage((uid, damage), comp.Damage, true, false);
            }
        while (queryB.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            if (comp.NextDamage > curTime)
                continue;

            if (comp.DamageCap != 0 && damage.TotalDamage >= comp.DamageCap)
                continue;

            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            // Damage them
            foreach (var allowedState in comp.AllowedStates)
            {
                if(allowedState == mobState.CurrentState)
                    _damageable.ChangeDamage((uid, damage), comp.Damage, true, false);
            }
        while (queryC.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            if (comp.NextDamage > curTime)
                continue;

            if (comp.DamageCap != 0 && damage.TotalDamage >= comp.DamageCap)
                continue;

            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            foreach (var allowedState in comp.AllowedStates)
            {
                if(allowedState == mobState.CurrentState)
                    _damageable.ChangeDamage((uid, damage), comp.Damage, true, false);
            }
        while (queryD.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            if (comp.NextDamage > curTime)
                continue;

            if (comp.DamageCap != 0 && damage.TotalDamage >= comp.DamageCap)
                continue;

            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            foreach (var allowedState in comp.AllowedStates)
            {
                if(allowedState == mobState.CurrentState)
                    _damageable.ChangeDamage((uid, damage), comp.Damage, true, false);
            }
        }
    }
}
