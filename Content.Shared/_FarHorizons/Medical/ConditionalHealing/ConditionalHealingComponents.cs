using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Medical.Healing;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Medical.ConditionalHealing;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ConditionalHealingData
{
    [DataField]
    public DamageSpecifier Damage = default!;
    [DataField]
    public float BloodlossModifier = 0.0f;
    [DataField]
    public float ModifyBloodLevel = 0.0f;
    [DataField]
    public List<ProtoId<DamageContainerPrototype>>? DamageContainers;
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3f);
    [DataField]
    public float SelfHealPenaltyMultiplier = 3f;
    [DataField]
    public SoundSpecifier? HealingBeginSound = null;
    [DataField]
    public SoundSpecifier? HealingEndSound = null;
    [DataField]
    public bool SolutionDrain = false;
    [DataField]
    public List<ReagentQuantity> ReagentsToDrain = [];

    [DataField]
    public int AdjustEyeDamage = 0;

    public HealingComponent MakeComponent(EntityUid owner) => // Euph - add owner
        new()
        {
            Damage = Damage,
            BloodlossModifier = BloodlossModifier,
            ModifyBloodLevel = ModifyBloodLevel,
            DamageContainers = DamageContainers,
            Delay = Delay,
            SelfHealPenaltyMultiplier = SelfHealPenaltyMultiplier,
            HealingBeginSound = HealingBeginSound,
            HealingEndSound = HealingEndSound,

            // Euph - this ai-generated system passes the HealingComponent straight to the healing system without adding it anywhere
            // This results in a debug assert in the constructor of Entity<T>
            // What the fuck?
            #pragma warning disable CS0618 // Type or member is obsolete
            Owner = owner,
        };
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ConditionalHealingDefition
{
    [DataField]
    public HashSet<ProtoId<TagPrototype>> AllowedTags = [];
    [DataField]
    public ConditionalHealingData Healing = default!;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConditionalHealingComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<ConditionalHealingDefition> HealingDefinitions = [];

}
