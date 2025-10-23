using Content.Shared._FarHorizons.Silicons.IPC;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles;

namespace Content.Server._FarHorizons.Silicons.IPC;

public sealed partial class IPCSystem
{
    protected override void SetupBattery()
    {
        base.SetupBattery();
        
        SubscribeLocalEvent<IPCBatteryComponent, StartingGearEquippedEvent>(OnBatteryStartingGear);
        SubscribeLocalEvent<IPCBatteryComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<IPCBatteryComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<IPCBatteryComponent, MobStateChangedEvent>(OnBatteryStateChange);
    }

    private void OnBatteryStateChange(Entity<IPCBatteryComponent> ent, ref MobStateChangedEvent args)
    {
        _powerCell.SetDrawEnabled(ent.Owner, !_state.IsDead(ent));
        UpdateUI(ent);
    }
        

    protected override void UpdateBatteryTimer(Entity<IPCBatteryComponent> ent)
    {
        if (!_state.IsAlive(ent))
            return;

        if (ent.Comp.NumWarnings > 0){
            var step = ent.Comp.DieWithoutPowerAfter / ent.Comp.NumWarnings;
            var should_send = Math.Ceiling(ent.Comp.NumWarnings - (ent.Comp.Timer / step));
            if (should_send > ent.Comp.WarningsIssued)
            {
                SendCriticalChargeWarning(ent);
                ent.Comp.WarningsIssued += 1;
            }
        }

        if (ent.Comp.Timer == 0 &&
            TryComp<MobStateComponent>(ent, out var state))
        {
            _state.ChangeMobState(ent, MobState.Dead, state);
        }
    }

    private void SendCriticalChargeWarning(Entity<IPCBatteryComponent> ent)
    {
        if(ent.Comp.WarningText != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.WarningText), ent, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.WarningSound, ent);
    }
    
    private void OnBatteryStartingGear(Entity<IPCBatteryComponent> ent, ref StartingGearEquippedEvent args)
    {
        if (!TryComp<BatteryDrainerComponent>(ent, out var drainer))
            return;

        ent.Comp.Battery = ent.Comp.BatteryContainerSlot.ContainedEntity;
        _drainer.SetBattery((ent, drainer), ent.Comp.BatteryContainerSlot.ContainedEntity);
        UpdateBatteryAlert(ent);
    }

    private void OnPowerCellSlotEmpty(Entity<IPCBatteryComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        StartDeathTimer(ent);
        UpdateBatteryAlert(ent);
        UpdateUI(ent);
    }
    private void OnPowerCellChanged(Entity<IPCBatteryComponent> ent, ref PowerCellChangedEvent args)
    {
        if(!_powerCell.HasDrawCharge(ent))
            StartDeathTimer(ent);
        else
            StopDeathTimer(ent);

        UpdateBatteryAlert(ent);
        UpdateUI(ent);
    }

    private static void StartDeathTimer(Entity<IPCBatteryComponent> ent){
        if (ent.Comp.TimerActive)
            return;
        
        ent.Comp.TimerActive = true;
        ent.Comp.WarningsIssued = 0;
        ent.Comp.Timer = ent.Comp.DieWithoutPowerAfter;
    }

    private static void StopDeathTimer(Entity<IPCBatteryComponent> ent){
        if (!ent.Comp.TimerActive)
            return;
        
        ent.Comp.TimerActive = false;
        ent.Comp.WarningsIssued = 0;
        ent.Comp.Timer = 0f;
    }

    private void UpdateBatteryAlert(Entity<IPCBatteryComponent> ent)
    {
        if (_state.IsAlive(ent) && ent.Comp.TimerActive && !_powerCell.HasDrawCharge(ent)){
            _alerts.ShowAlert(ent.Owner, ent.Comp.ChargeCritical);
        } else {
            _alerts.ClearAlert(ent.Owner, ent.Comp.ChargeCritical);
        }

        if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery, ent.Comp.PowerCellSlot))
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var chargePercent = (short) MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }

    public void DrainBattery(Entity<IPCBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) ||
            ent.Comp.Battery == null)
            return;

        _battery.SetCharge(ent.Comp.Battery.Value, 0);
    }

    public void EjectBattery(Entity<IPCBatteryComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp) ||
            ent.Comp.Battery == null)
            return;
        
        var battery = ent.Comp.Battery.Value;
        _container.EmptyContainer(ent.Comp.BatteryContainerSlot);

        _hands.PickupOrDrop(user, battery, dropNear: true);
    }
}