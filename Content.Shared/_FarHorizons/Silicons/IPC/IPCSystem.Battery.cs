using Content.Shared.Containers.ItemSlots;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Shared._FarHorizons.Silicons.IPC;

public abstract partial class SharedIPCSystem
{
    [Dependency] private readonly ILogManager _logs = default!;
    protected ISawmill _sawmill = default!;

    protected virtual void SetupBattery()
    {
        _sawmill = _logs.GetSawmill("IPC");

        SubscribeLocalEvent<IPCBatteryComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<IPCBatteryComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<IPCBatteryComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
    }

    private void UpdateBattery(float deltaTime)
    {
        // When battery runs out, we begin countdown and call events as it's ticking and another event when time has ran out
        var query = EntityQueryEnumerator<IPCBatteryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.TimerActive)
                continue;

            comp.Timer = Math.Max(comp.Timer - deltaTime, 0f);
            if (comp.Timer == 0f)
                comp.TimerActive = false;
            UpdateBatteryTimer((uid, comp));
        }
    }

    protected abstract void UpdateBatteryTimer(Entity<IPCBatteryComponent> ent);

    private void OnItemSlotEjectAttempt(Entity<IPCBatteryComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled ||
            !TryComp<PowerCellSlotComponent>(ent, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(ent, out var panel) ||
            !_items.TryGetSlot(ent, cellSlotComp.CellSlotId, out var cellSlot) ||
            cellSlot != args.Slot)
            return;

        if (!panel.Open)
            args.Cancelled = true;
    }

    private void OnItemSlotInsertAttempt(Entity<IPCBatteryComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled ||
            !TryComp<PowerCellSlotComponent>(ent, out var cellSlotComp) ||
            !TryComp<WiresPanelComponent>(ent, out var panel) ||
            !_items.TryGetSlot(ent, cellSlotComp.CellSlotId, out var cellSlot) ||
            cellSlot != args.Slot)
            return;

        if (!panel.Open)
            args.Cancelled = true;
    }

    private void OnBatteryStartup(Entity<IPCBatteryComponent> ent, ref ComponentStartup args) 
    {
        ent.Comp.PowerCellSlot = EnsureComp<PowerCellSlotComponent>(ent);
        ent.Comp.BatteryContainerSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.BatteryContainerSlotID);
        ent.Comp.BatteryDrainer = EnsureComp<BatteryDrainerComponent>(ent);
        EnsureComp<PowerCellDrawComponent>(ent);
    }

    public bool BatteryHasCharge(EntityUid uid) => _powerCell.HasDrawCharge(uid);
}