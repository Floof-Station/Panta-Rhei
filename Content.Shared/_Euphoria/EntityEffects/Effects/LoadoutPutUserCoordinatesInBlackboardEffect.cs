using System.Numerics;
using Content.Shared._Euphoria.NPC;
using Content.Shared.EntityEffects;
using Robust.Shared.Map;

namespace Content.Shared._Euphoria.EntityEffects.Effects;

/// <summary>
///     Puts the coordinates at the center of (and following) args.User into an HTN blackboard field.
///     Used in loadouts to tell an entity to follow the laodout owner.
/// </summary>
public sealed partial class LoadoutPutUserCoordinatesInBlackboardEffectSystem : EntityEffectSystem<MetaDataComponent, LoadoutPutUserCoordinatesInBlackboardEffect>
{
    [Dependency] private readonly SharedHtnHelperSystem _htnHelper = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<LoadoutPutUserCoordinatesInBlackboardEffect> args)
    {
        if (args.User is null)
            return;

        _htnHelper.SetBlackboard(
            entity,
            args.Effect.Key,
            new EntityCoordinates(args.User.Value, args.Effect.Offset));
    }
}

/// <inheritdoc cref="LoadoutLinkToEntitiesEffectSystem"/>
public sealed partial class LoadoutPutUserCoordinatesInBlackboardEffect : EntityEffectBase<LoadoutPutUserCoordinatesInBlackboardEffect>
{
    /// <summary>
    ///     Key to set.
    /// </summary>
    [DataField]
    public string Key;

    /// <summary>
    ///     Offset from the center of the user.
    /// </summary>
    [DataField]
    public Vector2 Offset;
}
