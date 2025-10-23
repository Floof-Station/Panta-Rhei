using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Silicons.IPC;

[Serializable, NetSerializable]
public enum IPCUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class IPCBuiState : BoundUserInterfaceState
{
    public float ChargePercent;

    public bool HasBattery;

    public MobState MobState;

    public int EyeDamage;

    public float BloodLevel;

    public DamageSpecifier Damage;

    public IPCBuiState(float chargePercent, bool hasBattery, MobState mobState, int eyeDamage, float bloodLevel, DamageSpecifier damage)
    {
        ChargePercent = chargePercent;
        HasBattery = hasBattery;
        MobState = mobState;
        EyeDamage = eyeDamage;
        BloodLevel = bloodLevel;
        Damage = damage;
    }
}

[Serializable, NetSerializable]
public sealed class IPCEjectBrainBuiMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class IPCEjectBatteryBuiMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class IPCSetNameBuiMessage : BoundUserInterfaceMessage
{
    public string Name;

    public IPCSetNameBuiMessage(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class IPCRequestUpdateBuiMessage : BoundUserInterfaceMessage;