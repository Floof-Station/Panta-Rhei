using Content.Server._Common.Consent;
using Content.Server.DoAfter;
using Content.Server.EUI;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared._HL.Brainwashing;
using Content.Shared.Clothing;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.StatusEffect;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;

namespace Content.Server._HL.Brainwashing;

public sealed class BrainwasherSystem : SharedBrainwasherSystem
{
    [Dependency] private readonly SharedBrainwashedSystem _sharedBrainwashedSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedFlashSystem _flashSystem = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ConsentSystem _consentSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize(); // HL: We've added an Init to the Shared System for the client-side verb drawing

        SubscribeLocalEvent<BrainwasherComponent, ClothingGotEquippedEvent>((uid, component, args) => StartBrainwashing(uid, args.Wearer, component));
        SubscribeLocalEvent<BrainwasherComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<BrainwasherComponent, EngagedEvent>(Engaged);
    }

    private void OnUnequipped(EntityUid uid, BrainwasherComponent component, ClothingGotUnequippedEvent args)
    {
        if (component.DoAfter == null)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;
    }

    private void Engaged(EntityUid uid, BrainwasherComponent component, EngagedEvent args)
    {
        component.DoAfter = null; // Informs the component the doAfter doesn't exist anymore

        if (args.Cancelled) // Now we can let the handlers do our doafter work again yippie :DD
            return;

        var user = _entityManager.GetEntity(args.Wearer);
        TryGetNetEntity(user, out var userNetEntity);
        if (userNetEntity == null)
            return;

        if (!component.BypassMindshield && HasComp<MindShieldComponent>(user))
        {
            _popupSystem.PopupCoordinates("Installation failed!", user.ToCoordinates());
            return;
        }

        _audioSystem.PlayPvs(component.EngageSound, uid, new AudioParams());
        _statusEffectsSystem.TryAddStatusEffect<FlashedComponent>(user,
            _flashSystem.FlashedKey,
            TimeSpan.FromSeconds(5),
            true);
        _stun.TrySlowdown(user, TimeSpan.FromSeconds(5), true, 0, 0);
        EnsureComp<BrainwashedComponent>(user, out var newBrainwashedComponent);
        _sharedBrainwashedSystem.SetCompulsions(user, newBrainwashedComponent, component.Compulsions);
        var brainwashedEvent = new BrainwashedEvent();
        RaiseLocalEvent(user, brainwashedEvent);
        RaiseNetworkEvent(brainwashedEvent, user);
    }

    private void StartBrainwashing(EntityUid uid, EntityUid target, BrainwasherComponent component)
    {
        if (!_consentSystem.HasConsent(target, "MindControl") || HasComp<BorgChassisComponent>(target))
            return;

        TryGetNetEntity(target, out var netEntity);
        if (netEntity == null || component.DoAfter != null)
            return;

        TryComp<DoAfterComponent>(target, out var doAfterComponent);
        var doAfterArgs = new DoAfterArgs(_entityManager,
            target,
            component.ChardingDuration,
            new EngagedEvent(netEntity.Value),
            uid)
        {
            RequireCanInteract = false,
        };

        if (HasComp<MobStateComponent>(uid))
        {
            doAfterComponent = null;
            if (TryComp<DoAfterComponent>(uid, out var newdoAfterComponent))
                doAfterComponent = newdoAfterComponent;

            doAfterArgs = new DoAfterArgs(_entityManager,
            uid,
            component.ChardingDuration,
            new EngagedEvent(netEntity.Value),
            uid)
            {
                RequireCanInteract = false,
                BreakOnMove = true,
                BreakOnDamage = true,
            };
        }

        if (doAfterComponent == null)
            return;

        var startDoAfterSuccess = _doAfterSystem.TryStartDoAfter(doAfterArgs, out var doAfterId, doAfterComponent);
        if (!startDoAfterSuccess)
            return;

        component.DoAfter = doAfterId;
        _audioSystem.PlayPvs(component.ChargingSound, uid, new AudioParams());
    }

    protected override void DoConfigureVerb(EntityUid uid, EntityUid user, BrainwasherComponent component)
    {
        var ui = new BrainwashEditor(_sharedBrainwashedSystem);
        if (!_playerManager.TryGetSessionByEntity(user, out var session))
            return;
        _euiManager.OpenEui(ui, session);
        ui.UpdateCompulsions(component, uid);
    }

    protected override void DoBrainwashingVerb(EntityUid uid, EntityUid target, BrainwasherComponent component)
    {
        StartBrainwashing(uid, target, component);
    }
}
