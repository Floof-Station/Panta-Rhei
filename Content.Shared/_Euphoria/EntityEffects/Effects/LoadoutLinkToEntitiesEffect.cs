using System.Linq;
using Content.Shared.DeviceLinking;
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Whitelist;

namespace Content.Shared._Euphoria.EntityEffects.Effects;

/// <summary>
///     Links the entity this effect is applied on to all entities equipped on the user.
///     Used in loadouts to e.g. link a remote to all shock collars of the user.
/// </summary>
public sealed partial class LoadoutLinkToEntitiesEffectSystem : EntityEffectSystem<MetaDataComponent, LoadoutLinkToEntitiesEffect>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelsts = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _links = default!;

    // NOTE: DO NOT EVER COPY-PASTE THIS EFFECT
    // If you need a similar behavior, make two abstract classes (something like BaseLoadoutOnOtherLoadoutsEffect and Base...System), move Effect() there, and make Link() abstract!
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<LoadoutLinkToEntitiesEffect> args)
    {
        var effect = args.Effect;
        if (!TryComp<InventoryComponent>(args.User, out var inventory))
            return;

        // Collect the list of all entities that may need processing using BFS
        // If the entity satisfies the white/blacklist, we link it, otherwise we prcoess all of its contents if it's a storage
        var toProcessQueue = new Queue<EntityUid?>(inventory.Containers.Select(it => it.ContainedEntity));
        while (toProcessQueue.TryDequeue(out var maybeNext))
        {
            if (maybeNext is not { } next)
                continue;

            if (!_whitelsts.IsWhitelistFail(effect.Whitelist, next)
                && !_whitelsts.IsWhitelistPass(effect.Blacklist, next))
            {
                Link(entity, next, effect.LinkThisToOther, effect.LinkOtherToThis);
                continue;
            }

            if (TryComp<StorageComponent>(next, out var nextStorage))
            {
                foreach (var containedInNext in nextStorage.Container.ContainedEntities)
                    toProcessQueue.Enqueue(containedInNext);
            }
        }
    }

    private void Link(EntityUid a, EntityUid b, bool linkAToB, bool linkBToA)
    {
        // A is always the loadout item. We assume it has the relevant components and let LinkDefaults log an error if it doesn't
        if (linkAToB && TryComp<DeviceLinkSinkComponent>(b, out var bSink))
            _links.LinkDefaults(null, a, b, sinkComponent: bSink);

        if (linkBToA && TryComp<DeviceLinkSourceComponent>(b, out var bSource))
            _links.LinkDefaults(null, b, a, sourceComponent: bSource);
    }
}

/// <inheritdoc cref="LoadoutLinkToEntitiesEffectSystem"/>
public sealed partial class LoadoutLinkToEntitiesEffect : EntityEffectBase<LoadoutLinkToEntitiesEffect>
{
    [DataField]
    public EntityWhitelist? Whitelist, Blacklist;

    /// <summary>
    ///     Link A to B means "when A is activated, activate B".
    ///     This = the loadout item, Other = an item that satisfies the whitelists.
    /// </summary>
    [DataField]
    public bool LinkThisToOther, LinkOtherToThis;
}
