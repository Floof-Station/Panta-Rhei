using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.HTN;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Bots;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.NPC.Operators.Specific;

// Euph - most of this shitcode was rewritten by me
// Idk who came up with the original code but it was beyond awful
public sealed partial class WeldbotWeldOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ChatSystem _chat = default!;
    private SharedAudioSystem _audio = default!;
    private SharedInteractionSystem _interaction = default!;
    private DamageableSystem _damageableSystem = default!;
    private TagSystem _tagSystem = default!;

    public static readonly ProtoId<TagPrototype> SiliconTag = "SiliconMob";
    public static readonly ProtoId<TagPrototype> WeldotFixableStructureTag = "WeldbotFixableStructure";

    /// <summary>
    /// Target entity to inject.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField]
    public DamageSpecifier StructureHealing = new()
    {
        DamageDict =
        {
            { "Structural", -3 },
        }
    };

    [DataField]
    public DamageSpecifier SiliconHealing = new()
    {
        DamageDict =
        {
            { "Blunt", -5 },
            { "Slash", -5 },
            { "Piercing", -5 },
        }
    };

    [DataField]
    public DamageSpecifier EmaggedDamage = new()
    {
        DamageDict =
        {
            { "Heat", -10 },
        }
    };

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = sysManager.GetEntitySystem<ChatSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _damageableSystem = sysManager.GetEntitySystem<DamageableSystem>();
        _tagSystem = sysManager.GetEntitySystem<TagSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) || _entMan.Deleted(target))
            return HTNOperatorStatus.Failed;

        var tagSiliconMobPrototype = _prototypeManager.Index(SiliconTag);
        var tagWeldFixableStructurePrototype = _prototypeManager.Index(WeldotFixableStructureTag);

        if(!_entMan.TryGetComponent<TagComponent>(target, out var tagComponent))
            return HTNOperatorStatus.Failed;

        var weldableIsSilicon = _tagSystem.HasTag(tagComponent, tagSiliconMobPrototype);
        var weldableIsStructure = _tagSystem.HasTag(tagComponent, tagWeldFixableStructurePrototype);

        if ((!weldableIsSilicon && !weldableIsStructure)
            || !_entMan.TryGetComponent<WeldbotComponent>(owner, out var botComp)
            || !_entMan.TryGetComponent<DamageableComponent>(target, out var damageable)
            || !_interaction.InRangeUnobstructed(owner, target))
            return HTNOperatorStatus.Failed;

        var damage = _damageableSystem.GetPositiveDamage((target, damageable));

        var canWeldSilicon = DamageIntersects(damage, SiliconHealing) || _entMan.HasComponent<EmaggedComponent>(owner);
        var canWeldStructure = DamageIntersects(damage, StructureHealing);

        if ((!canWeldSilicon && weldableIsSilicon) || (!canWeldStructure && weldableIsStructure))
            return HTNOperatorStatus.Failed;

        DamageSpecifier damageChange;
        if (botComp.IsEmagged)
            damageChange = EmaggedDamage;
        else
        {
            if (weldableIsSilicon)
                damageChange = SiliconHealing;
            else if (weldableIsStructure)
                damageChange = StructureHealing;
            else
                return HTNOperatorStatus.Failed; // Shouldn't happen?
        }

        if (!_damageableSystem.TryChangeDamage(target, damageChange, true, false))
            return HTNOperatorStatus.Failed;

        _audio.PlayPvs(botComp.WeldSound, target);
        _chat.TrySendInGameICMessage(owner, Loc.GetString("weldbot-finish-weld"), InGameICChatType.Speak, hideChat: true, hideLog: true);

        return HTNOperatorStatus.Finished;
    }

    // I'm tired of this BS
    public static bool DamageIntersects(DamageSpecifier toHeal, DamageSpecifier healing) =>
        DamageIntersects(toHeal, healing.DamageDict.Keys);

    public static bool DamageIntersects(DamageSpecifier toHeal, IEnumerable<ProtoId<DamageTypePrototype>> healing) =>
        healing.Any(it => toHeal.DamageDict.ContainsKey(it));
}
