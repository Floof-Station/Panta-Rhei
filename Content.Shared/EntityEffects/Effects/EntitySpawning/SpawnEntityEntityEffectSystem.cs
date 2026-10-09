using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a number of entities of a given prototype at the coordinates of this entity.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpawnEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntity>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntity> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var proto = args.Effect.Entity;

        if (args.Effect.Predicted)
        {
            for (var i = 0; i < quantity; i++)
            {
                //Euphoria  was: PredictedSpawnNextToOrDrop(proto, entity, entity.Comp);
                SpawnNextToOrDropAtPosition(proto, entity, entity.Comp);
            }
        }
        else if (_net.IsServer)
        {
            for (var i = 0; i < quantity; i++)
            {
                SpawnNextToOrDrop(proto, entity, entity.Comp);
            }
        }
    }
    // Euphoria changes start - we need to makey this spawny avoid the fishspess
    private EntityUid SpawnNextToOrDropAtPosition(string? protoName, EntityUid target, TransformComponent? xform = null, ComponentRegistry? overrides = null)
    {
        xform ??= Transform(target);

        if (!xform.ParentUid.IsValid())
        {
            Log.Error($"Tried to spawn {protoName} in nullspace.");
                return EntityUid.Invalid;
        }

        var uid = PredictedSpawnAtPosition(protoName, xform.Coordinates, overrides);

        _xforms.DropNextTo(uid, target);

        return uid;
    }
    // Euphoria changes end
}

/// <inheritdoc cref="BaseSpawnEntityEntityEffect{T}"/>
public sealed partial class SpawnEntity : BaseSpawnEntityEntityEffect<SpawnEntity>;
