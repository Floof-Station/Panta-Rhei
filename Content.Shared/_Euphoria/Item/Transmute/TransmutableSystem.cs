using Content.Shared.Cloning;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Content.Shared._Euphoria.Item.Transmute;
public sealed class TransmutableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCloningSystem _cloning = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityManager _entity = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TransmutableComponent, TransmutableDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<TransmutableComponent, TransmutableBoundUserInterfaceMessage>(OnBoundUserInterfaceMessage);
    }

    private void OnDoAfter(Entity<TransmutableComponent> transmutable, ref TransmutableDoAfterEvent args)
    {
        TransmuteEntity(transmutable, args.Prototype);
    }

    private void OnBoundUserInterfaceMessage(Entity<TransmutableComponent> transmutable, ref TransmutableBoundUserInterfaceMessage args)
    {
        if (!transmutable.Comp.AvailablePrototypes.Contains(args.Prototype.Id) ||
            !_prototype.HasIndex(args.Prototype))
            return;

        StartTransmuteEntity(transmutable, args.Prototype, GetEntity(args.User));
    }

    /// <summary>
    /// Start transmuting a transmutable entity into another entity, respecting delay. Plays sound if applicable.
    /// </summary>
    private void StartTransmuteEntity(Entity<TransmutableComponent> transmutable, EntProtoId prototype, EntityUid user)
    {
        var comp = transmutable.Comp;

        // Would use PlayPredicted but this is only running server side, and that would result in the user not hearing it.
        if (comp.PlayTransmuteSound && _net.IsServer)
            _audio.PlayPvs(comp.TransmuteSound, Transform(transmutable).Coordinates);

        if (comp.Delay == 0)
        {
            TransmuteEntity(transmutable, prototype);
            return;
        }

        var doAfterEvent = new TransmutableDoAfterEvent(prototype);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, comp.Delay, doAfterEvent, transmutable, used: transmutable)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// Transmutes the given entity into a new entity of the given prototype. Transfers components from the original to the new entity based on the TransmutableComponent whitelist.
    /// </summary>
    private void TransmuteEntity(Entity<TransmutableComponent> transmutable, EntProtoId prototype)
    {
        TryReplaceEntity(transmutable.Owner, prototype, out _, transmutable.Comp.CloningSettingsId);
    }

    /// <summary>
    /// Replaces an entity with a new entity in place. Keeps the mind attached to the new entity. Returns true if the new entity is created.
    /// </summary>
    private bool TryReplaceEntity(EntityUid oldEntity, EntProtoId prototype, [NotNullWhen(true)] out EntityUid? newEntity, ProtoId<CloningSettingsPrototype>? cloningSettings = null, bool transferInventory = true, bool transferMind = true)
    {
        newEntity = null;

        if (_entity.IsQueuedForDeletion(oldEntity))
            return false;

        if (_container.TryGetContainingContainer(oldEntity, out var container))
        {
            _container.Remove(oldEntity, container);
            newEntity = PredictedSpawnInContainerOrDrop(prototype, container.Owner, container.ID);
        }
        else
        {
            newEntity = PredictedSpawnAttachedTo(prototype, Transform(oldEntity).Coordinates);
        }

        if (cloningSettings != null)
            _cloning.CloneComponents(oldEntity, newEntity.Value, cloningSettings.Value);

        if (transferMind && _mind.TryGetMind(oldEntity, out var mindId, out var mindComp))
            _mind.TransferTo(mindId, newEntity, mind: mindComp);

        _entity.PredictedDeleteEntity(oldEntity);

        return true;
    }
}

[Serializable, NetSerializable]
public sealed class TransmutableBoundUserInterfaceMessage(EntProtoId prototype, NetEntity user) : BoundUserInterfaceMessage
{
    public EntProtoId Prototype = prototype;
    public NetEntity User = user;
}

[Serializable, NetSerializable]
public sealed partial class TransmutableDoAfterEvent : DoAfterEvent
{
    public EntProtoId Prototype;

    public TransmutableDoAfterEvent(EntProtoId prototype)
    {
        Prototype = prototype;
    }

    public override DoAfterEvent Clone()
    {
        return new TransmutableDoAfterEvent(Prototype);
    }
}

[Serializable, NetSerializable]
public enum TransmutableUiKey : byte
{
    Key
}
