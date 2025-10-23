using Content.Server.Ghost;
using Content.Shared._FarHorizons.Silicons.IPC;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Shared.Utility;

namespace Content.Server._FarHorizons.Silicons.IPC;

public sealed partial class IPCSystem
{
    protected override void SetupRevive()
    {
        base.SetupRevive();

        SubscribeLocalEvent<IPCReviveComponent, TargetBeforeDefibrillatorZapsEvent>(OnBeforeZap);
        SubscribeLocalEvent<IPCReviveComponent, IPCRebootDoAfterEvent>(OnReviveDoAfter);
    }

    private void OnReviveDoAfter(Entity<IPCReviveComponent> ent, ref IPCRebootDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        FinishReboot(ent);   
    }

    private void OnBeforeZap(Entity<IPCReviveComponent> ent, ref TargetBeforeDefibrillatorZapsEvent args)
    {
        if (args.Cancelled ||
            !TryComp<DefibrillatorComponent>(args.Defib, out var defib))
            return;

        if (ent.Comp.DefibDamage != null)
            _damageable.TryChangeDamage(ent, ent.Comp.DefibDamage);
        
        if (ent.Comp.DefibBatteryDrain)
            DrainBattery((ent, null));

        _audio.PlayPvs(defib.ZapSound, args.Defib);
        args.Cancel();
    }

    private void AddReviveVerbs(GetVerbsEvent<Verb> ev)
    {
        if (!ev.CanInteract || !ev.CanAccess || !ev.CanComplexInteract ||
            !TryComp<IPCReviveComponent>(ev.Target, out var revive) ||
            !TryComp<WiresPanelComponent>(ev.Target, out var wires) ||
            !wires.Open ||
            !revive!.RebootButton ||
            !_state.IsDead(ev.Target))
            return;

        var verb = new Verb
        {
            Text = "Reboot",
            Category = new("IPC", "/Textures/Interface/VerbIcons/group.svg.192dpi.png"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Act = () => StartReboot((ev.Target, revive)),
        };

        ev.Verbs.Add(verb);
        
    }

    public void StartReboot(Entity<IPCReviveComponent> ent)
    {
        if (!ent.Comp.RebootButton)
            return;

        if (!TryComp<DamageableComponent>(ent, out var damageableComponent) ||
            !_mobThreshold.TryGetThresholdForState(ent, MobState.Dead, out var thresholdDead) ||
            damageableComponent.TotalDamage > thresholdDead || 
            !BatteryHasCharge(ent))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.CantReviveMessage), ent);
            _audio.PlayPvs(ent.Comp.RebootFailSound, ent);
            return;
        }
        
        _popup.PopupEntity(Loc.GetString(ent.Comp.RebootingMessage), ent);
        _audio.PlayPvs(ent.Comp.RebootSound, ent);

        if (ent.Comp.RebootTime == TimeSpan.Zero)
            FinishReboot(ent);
        else
        {
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.RebootTime, new IPCRebootDoAfterEvent(), ent)
            {
                Hidden = false,
                NeedHand = false,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                BreakOnDamage = true,
                CancelDuplicate = true,
                RequireCanInteract = false,
            });
        }
    }

    private void FinishReboot(Entity<IPCReviveComponent> ent)
    {
        var dead = false;
        var hasPlayer = false;

        if (TryComp<UnrevivableComponent>(ent, out var unrevivable))
        {
            _popup.PopupEntity(Loc.GetString(unrevivable.ReasonMessage), ent);
            return;
        }

        if (TryComp<DamageableComponent>(ent, out var damageableComponent) &&
            _mobThreshold.TryGetThresholdForState(ent, MobState.Dead, out var thresholdDead) &&
            _mobThreshold.TryGetThresholdForState(ent, MobState.Critical, out var thresholdCrit))
        {
            if (damageableComponent.TotalDamage < thresholdCrit)
                _state.ChangeMobState(ent, MobState.Alive);
            else if (damageableComponent.TotalDamage < thresholdDead)
                _state.ChangeMobState(ent, MobState.Critical);
        } else
            dead = true;

        if (_mind.TryGetMind(ent, out _, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var playerSession))
        {
            hasPlayer = true;

            if (mind.CurrentEntity != ent)
            {
                _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind, _player), playerSession);
            }
        }

        var sound = dead || !hasPlayer
            ? ent.Comp.RebootFailSound
            : ent.Comp.RebootSuccessSound;
        _audio.PlayPvs(sound, ent);
    }
}