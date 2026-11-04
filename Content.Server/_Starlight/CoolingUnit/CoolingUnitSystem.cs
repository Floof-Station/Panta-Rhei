using Content.Server.Body.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._Starlight.CoolingUnit;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.CoolingUnit;

public sealed partial class CoolingUnitSystem : SharedCoolingUnitSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    private TimeSpan _nextUpdate;
    private readonly TimeSpan _updateCooldown = TimeSpan.FromSeconds(1);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime <= _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + _updateCooldown;

        var query = EntityQueryEnumerator<TemperatureComponent, ThermalRegulatorComponent>();
        while (query.MoveNext(out var uid, out var temperature, out var regulator))
        {
            if (!_inventory.TryGetSlots(uid, out var slots))
                continue;

            foreach (var slot in slots)
            {
                if (!_inventory.TryGetSlotEntity(uid, slot.Name, out var item) ||
                    !TryComp<CoolingUnitComponent>(item, out var coolingUnit) ||
                    !TryComp<ItemToggleComponent>(item, out var toggle) ||
                    !toggle.Activated ||
                    temperature.CurrentTemperature <= regulator.NormalBodyTemperature)
                {
                    continue;
                }

                var cooling = Math.Min(coolingUnit.MaxCooling,
                    temperature.CurrentTemperature - regulator.NormalBodyTemperature);
                _temperature.ChangeHeat(uid, -cooling, true, temperature);
            }
        }
    }
}
