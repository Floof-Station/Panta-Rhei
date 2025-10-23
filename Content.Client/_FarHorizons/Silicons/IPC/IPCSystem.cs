using Content.Shared._FarHorizons.Silicons.IPC;

namespace Content.Client._FarHorizons.Silicons.IPC;

public sealed partial class IPCSystem : SharedIPCSystem
{
    protected override void UpdateBatteryTimer(Entity<IPCBatteryComponent> ent) { }

}