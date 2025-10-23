using System.Linq;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Silicons.HumanoidEMP;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class HumanoidEMPEffect
{
    [DataField]
    public TimeSpan StunAmount = default!;

    [DataField]
    public DamageSpecifier DamageAmount = new();

    [DataField]
    public TimeSpan BlindAmount = default!;

    [DataField]
    public TimeSpan KnockdownAmount = default!;

    [DataField]
    public TimeSpan SlowdownAmount = default!;

    [DataField]
    public float WalkSpeedModifier = 1f;

    [DataField]
    public float SprintSpeedModifier = 1f;

    [DataField]
    public List<string> DropItemsFrom = [];

    [DataField]
    public EntProtoId BlindStatusEffect = "StatusEffectBlinded";

    public static HumanoidEMPEffect operator +(HumanoidEMPEffect a, HumanoidEMPEffect b) => new()
    {
        StunAmount = a.StunAmount + b.StunAmount,
        DamageAmount = a.DamageAmount + b.DamageAmount,
        BlindAmount = a.BlindAmount + b.BlindAmount,
        KnockdownAmount = a.KnockdownAmount + b.KnockdownAmount,
        SlowdownAmount = a.SlowdownAmount + b.SlowdownAmount,
        WalkSpeedModifier = Math.Min(a.WalkSpeedModifier, b.WalkSpeedModifier),
        SprintSpeedModifier = Math.Min(a.SprintSpeedModifier, b.SprintSpeedModifier),
        DropItemsFrom = [.. a.DropItemsFrom.Union(b.DropItemsFrom)],
        BlindStatusEffect = a.BlindStatusEffect,
    };
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanoidEMPComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public HumanoidEMPEffect Effect;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanoidEMPCompositeElementComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public HumanoidEMPEffect Effect;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class HumanoidEMPCompositeComponent : Component;