using Content.Shared.Damage;

namespace Content.Shared._FarHorizons.Silicons.HumanoidEMP;

[RegisterComponent]
public sealed partial class OrganEmpTargetComponent : Component
{
    [DataField] public DamageSpecifier BaseDamage = new();
    [DataField] public Dictionary<int, HumanoidEMPEffect> Thresholds = [];
}

[ByRefEvent]
public record struct GatherOrganEmpEffectEvent(HumanoidEMPEffect Effect, int Strength);