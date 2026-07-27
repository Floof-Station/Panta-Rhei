using Content.Server._Common.Consent;
using Content.Shared._CS.Traits.Abilities;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.DoAfter;
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
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AphrodesiacBiteComponent, AphrodesiacBiteEvent>(OnBite);
        SubscribeLocalEvent<AphrodesiacBiteComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AphrodesiacBiteComponent, ABDoafterEvent>(TryFinishDoafter);
    }

    public void OnInit(EntityUid uid, AphrodesiacBiteComponent component, ComponentInit args) => _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);

    public void OnBite(Entity<AphrodesiacBiteComponent> ent, ref AphrodesiacBiteEvent args)
    {
        Log.Info("starting doafter");
        if (args.Handled)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, 0.75F, new ABDoafterEvent(), ent, target: args.Target));
        args.Handled = true;
    }
    
    private void TryFinishDoafter(Entity<AphrodesiacBiteComponent> ent, ref ABDoafterEvent args)
    {
        if (args.Args.Target is not { } target)
            return;
        if (args.Cancelled || args.Handled)
            return;
        args.Handled = true;
        
        Log.Info("attempting bite");
        args.Handled |= TryInject(ent.Comp, target, args.User);
    }

    public bool TryInject(AphrodesiacBiteComponent bite, EntityUid target, EntityUid user)
    {
        
        Log.Info("starting inject");
        if (!TryComp<BloodstreamComponent>(target, out _))
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
