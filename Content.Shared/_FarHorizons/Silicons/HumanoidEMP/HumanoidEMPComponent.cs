using System.Linq;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Silicons.HumanoidEMP;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class  HumanoidEMPEffect
{
    [DataField] public TimeSpan StunAmount = TimeSpan.Zero;

    [DataField] public TimeSpan KnockdownAmount = TimeSpan.Zero;

    [DataField] public TimeSpan SlowdownAmount = TimeSpan.Zero;

    [DataField] public float WalkSpeedModifier = 1f;

    [DataField] public float SprintSpeedModifier = 1f;

    [DataField] public List<string> DropItemsFrom = [];

    [DataField] public Dictionary<EntProtoId, TimeSpan> AdditionalEffects = [];

    [DataField] public TimeSpan GlitchDuration = TimeSpan.Zero;

    public static HumanoidEMPEffect operator +(HumanoidEMPEffect a, HumanoidEMPEffect b) => new()
    {
        StunAmount = a.StunAmount + b.StunAmount,
        KnockdownAmount = a.KnockdownAmount + b.KnockdownAmount,
        SlowdownAmount = a.SlowdownAmount + b.SlowdownAmount,
        WalkSpeedModifier = Math.Min(a.WalkSpeedModifier, b.WalkSpeedModifier),
        SprintSpeedModifier = Math.Min(a.SprintSpeedModifier, b.SprintSpeedModifier),
        DropItemsFrom = [.. a.DropItemsFrom.Union(b.DropItemsFrom)],
        AdditionalEffects = CombineEffects(a.AdditionalEffects, b.AdditionalEffects),
        GlitchDuration = a.GlitchDuration + b.GlitchDuration
    };

    public static Dictionary<EntProtoId, TimeSpan> CombineEffects(Dictionary<EntProtoId, TimeSpan> a, Dictionary<EntProtoId, TimeSpan> b)
    {
        var res = a;
        foreach (var (key, value) in b)
        {
            if (res.TryGetValue(key, out _))
                res[key] += value;
            else
                res[key] = value;
        }
        return res;
    }
}

[RegisterComponent]
public sealed partial class HumanoidEMPComponent : Component
{
    [DataField] public Dictionary<int, HumanoidEMPEffect> Thresholds = [];
    [DataField] public DamageSpecifier BaseDamage = new();

    [DataField] public TimeSpan EffectCooldown = TimeSpan.FromSeconds(0);

    public TimeSpan NextEffect = TimeSpan.FromSeconds(0);
}
