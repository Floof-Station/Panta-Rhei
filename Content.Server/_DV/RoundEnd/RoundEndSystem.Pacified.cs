using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.Trigger.Components;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared._DV.CCVars;
using Content.Shared.Damage.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Projectiles;
using Content.Shared.Store.Components;
using Content.Shared.Weapons.Melee;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.DeltaV.RoundEnd;

public sealed class PacifiedRoundEnd : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();
        _configurationManager.OnValueChanged(DCCVars.RoundEndPacifist, v => _enabled = v, true);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
    }

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        if (!_enabled)
            return;

        var harmQuery = EntityQueryEnumerator<CombatModeComponent>();
        while (harmQuery.MoveNext(out var uid, out var _))
        {
            EnsureComp<PacifiedComponent>(uid);
        }

        var explosiveQuery = EntityQueryEnumerator<ExplosiveComponent>();
        while (explosiveQuery.MoveNext(out var uid, out var _))
        {
            RemCompDeferred<ExplosiveComponent>(uid); // Floofstation changed to RemCompDeferred
        }

        //Floofstation EORG prevent additions begin
        // we need to account for all the types of grenades, to my knowledge all have a TimerTriggerComp
        var grenadeQuery = _entityManager.EntityQueryEnumerator<TimerTriggerComponent>();
        while (grenadeQuery.MoveNext(out var uid, out _))
        {
            RemCompDeferred<TimerTriggerComponent>(uid);
        }

        var flashQuery = EntityQueryEnumerator<FlashComponent>();
        while (flashQuery.MoveNext(out var uid, out var _))
        {
            RemCompDeferred<FlashComponent>(uid);
        }

        // we also need to account for melee weapons as well
        var meleeQuery = EntityQueryEnumerator<MeleeWeaponComponent>();
        while (meleeQuery.MoveNext(out var uid, out var meleeWeapon))
        {
            if (meleeWeapon.Damage.AnyPositive())
                continue;
            var totaldmg = meleeWeapon.Damage.GetTotal();
            if (totaldmg > 0)
                RemCompDeferred<MeleeWeaponComponent>(uid);
        }
        //Floofstation EORG prevent additions end

        var uplinkQuery = EntityQueryEnumerator<StoreComponent>();
        while (uplinkQuery.MoveNext(out var uid, out var store))
        {
            store.FullListingsCatalog.Clear();
            store.LastAvailableListings.Clear();
        }
    }
}
