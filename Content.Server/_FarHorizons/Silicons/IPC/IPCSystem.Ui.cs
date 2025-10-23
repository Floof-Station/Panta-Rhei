using Content.Shared._FarHorizons.Silicons.IPC;
using Content.Shared.Body.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.UserInterface;

namespace Content.Server._FarHorizons.Silicons.IPC;

public sealed partial class IPCSystem
{
    // CCvar.
    private int _maxNameLength;

    private void InitializeUI()
    {
        SubscribeLocalEvent<IPCLockComponent, BeforeActivatableUIOpenEvent>(OnBeforeIPCUiOpen);
        SubscribeLocalEvent<IPCLockComponent, DamageChangedEvent>( (ent, _, _) => UpdateUI(ent));

        SubscribeLocalEvent<IPCLockComponent, IPCEjectBrainBuiMessage>(OnEjectBrainBuiMessage);
        SubscribeLocalEvent<IPCLockComponent, IPCEjectBatteryBuiMessage>(OnEjectBatteryBuiMessage);
        SubscribeLocalEvent<IPCLockComponent, IPCSetNameBuiMessage>(OnSetNameBuiMessage);

        Subs.CVar(_cfgManager, CCVars.MaxNameLength, value => _maxNameLength = value, true);
    }

    private void OnEjectBrainBuiMessage(Entity<IPCLockComponent> ent, ref IPCEjectBrainBuiMessage args) =>
        EjectBrain(ent.Owner, args.Actor);
    private void OnEjectBatteryBuiMessage(Entity<IPCLockComponent> ent, ref IPCEjectBatteryBuiMessage args) =>
        EjectBattery(ent.Owner, args.Actor);
    private void OnSetNameBuiMessage(Entity<IPCLockComponent> ent, ref IPCSetNameBuiMessage args)
    {
        if (args.Name.Length > _maxNameLength ||
            args.Name.Length == 0 ||
            string.IsNullOrWhiteSpace(args.Name) ||
            string.IsNullOrEmpty(args.Name))
            return;

        var name = args.Name.Trim();

        var metaData = MetaData(ent);

        if (metaData.EntityName.Equals(name, StringComparison.InvariantCulture))
            return;

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Actor):player} set IPC \"{ToPrettyString(ent)}\"'s name to: {name}");
        _metaData.SetEntityName(ent, name, metaData);
    }

    private void OnBeforeIPCUiOpen(Entity<IPCLockComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    public void UpdateUI(EntityUid uid)
    {
        if (!_ui.IsUiOpen(uid, IPCUiKey.Key))
            return;

        var chargePercent = 0f;
        var hasBattery = false;
        var eyeDamage = 0;
        var bloodLevel = 0f;
        DamageSpecifier damage = new();
        if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
        {
            hasBattery = true;
            chargePercent = battery.CurrentCharge / battery.MaxCharge;
        }

        if (TryComp<DamageableComponent>(uid, out var damageable))
            damage = damageable.Damage;

        if (TryComp<BlindableComponent>(uid, out var blindable))
            eyeDamage = blindable.EyeDamage;

        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) &&
            _solutionContainerSystem.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                bloodLevel = bloodSolution.FillFraction;

        if (TryComp<MobStateComponent>(uid, out var mobState))
            _ui.SetUiState(uid, IPCUiKey.Key,
                new IPCBuiState(chargePercent, hasBattery, mobState.CurrentState, eyeDamage, bloodLevel, damage));
    }
} 