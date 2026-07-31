    using Content.Server._Common.Consent;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._CS.Traits.Abilities;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server._CS.Traits.Abilities;

public sealed class AphrodesiacBiteSystem : EntitySystem
{
    [Dependency] private readonly ConsentSystem _consent = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AphrodesiacBiteEvent>(OnBite);
        SubscribeLocalEvent<AphrodesiacBiteComponent, ComponentInit>(OnInit);
    }

    public void OnInit(EntityUid uid, AphrodesiacBiteComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    public void OnBite(AphrodesiacBiteEvent ev)
    {
        if (ev.Handled)
            return;

        if (TryInject(ev.Target, ev.Performer))
            ev.Handled = true;
    }

    public bool TryInject(EntityUid target, EntityUid user)
    {
        if (!TryComp<BloodstreamComponent>(target, out _))
            return false;

        if (!TryComp<AphrodesiacBiteComponent>(user, out var bite))
            return false;

        if (bite.RequiresConsent && !_consent.HasConsent(target, bite.ConsentToggleId))
        {
            _popup.PopupEntity(Loc.GetString("aphrodesiac-no-consent", ("target", target)), user, PopupType.LargeCaution);
            return false;
        }

        var solution = new Solution(bite.Reagent, bite.Amount);
        if (_bloodstream.TryAddToBloodstream(target, solution))
        {
            _audio.PlayPvs(bite.Sound, user);
            _actions.StartUseDelay(bite.ActionEntity);
            return true;
        }

        return false;
    }
}
