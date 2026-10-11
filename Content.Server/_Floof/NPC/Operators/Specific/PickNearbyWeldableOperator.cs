using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Bots;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.NPC.Operators.Specific;

// Euph - most of this has has, once again, been rewritten
public sealed partial class PickNearbyWeldableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private TagSystem _tagSystem = default!;
    private DamageableSystem _damageableSystem = default!;

    /// <summary>
    ///     Which damage types this npc is allowed to heal. Must match those in WeldbotWeldOperator to avoid conflicts.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> HealableDamageTypes = new() { "Structural", "Blunt", "Slash", "Piercing" };

    [DataField]
    public string RangeKey = NPCBlackboard.WeldbotWeldRange;

    /// <summary>
    /// Target entity to weld
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _tagSystem = sysManager.GetEntitySystem<TagSystem>();
        _damageableSystem = sysManager.GetEntitySystem<DamageableSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager) || !_entManager.TryGetComponent<WeldbotComponent>(owner, out var weldbot))
            return (false, null);

        var damageQuery = _entManager.GetEntityQuery<DamageableComponent>();
        var emagged = _entManager.HasComponent<EmaggedComponent>(owner);

        foreach (var target in _lookup.GetEntitiesInRange(owner, range))
        {
            if (!damageQuery.TryGetComponent(target, out var damageable)
                || !_entManager.TryGetComponent<TagComponent>(target, out var tagComponent))
                continue;

            var damage = _damageableSystem.GetPositiveDamage((target, damageable));

            // TODO: code duplication with WeldbotWeldOperator, but i dont want to rewrite it
            var hasDamage = WeldbotWeldOperator.DamageIntersects(damage, HealableDamageTypes);
            var canWeldSiliconMob = _tagSystem.HasTag(tagComponent, WeldbotWeldOperator.SiliconTag) && hasDamage;
            var canWeldStructure = _tagSystem.HasTag(tagComponent, WeldbotWeldOperator.WeldotFixableStructureTag) && hasDamage;
            if(!canWeldSiliconMob && !canWeldStructure)
                continue;

            var pathRange = SharedInteractionSystem.InteractionRange;

            //Needed to make sure it doesn't sometimes stop right outside its interaction range, in case of a mob.
            if (canWeldSiliconMob)
                pathRange--;

            var path = await _pathfinding.GetPath(owner, target, pathRange, cancelToken);
            if (path.Result == PathResult.NoPath)
                continue;

            return (true, new()
            {
                {TargetKey, target},
                {TargetMoveKey, _entManager.GetComponent<TransformComponent>(target).Coordinates},
                {NPCBlackboard.PathfindKey, path},
            });
        }

        return (false, null);
    }
}
