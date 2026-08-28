using Content.Shared._Euphoria.NPC;
using Content.Shared.EntityEffects;

namespace Content.Shared._Euphoria.EntityEffects.Effects;

/// <summary>
///     Sets an HTN blackboard field to args.User. Used in loadouts to tell an entity to follow the laodout owner.
/// </summary>
public sealed partial class LoadoutSetHtnBlackboardToUserEffectSystem : EntityEffectSystem<MetaDataComponent, LoadoutSetHtnBlackboardToUserEffect>
{
    [Dependency] private readonly SharedHtnHelperSystem _htnHelper = default!;

    // NOTE: DO NOT EVER COPY-PASTE THIS EFFECT
    // If you need a similar behavior, make two abstract classes (something like BaseLoadoutOnOtherLoadoutsEffect and Base...System), move Effect() there, and make Link() abstract!
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<LoadoutSetHtnBlackboardToUserEffect> args)
    {
        if (args.User is null)
            return;

        _htnHelper.SetBlackboard(entity, args.Effect.Key, args.User);
    }
}

/// <inheritdoc cref="LoadoutLinkToEntitiesEffectSystem"/>
public sealed partial class LoadoutSetHtnBlackboardToUserEffect : EntityEffectBase<LoadoutSetHtnBlackboardToUserEffect>
{
    /// <summary>
    ///     Key to set.
    /// </summary>
    [DataField]
    public string Key;
}
