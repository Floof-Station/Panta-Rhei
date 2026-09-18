using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Shared._Euphoria.Item.Transmute;
public sealed class TransmutableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityManager _entity = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransmutableComponent, TransmutableDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<TransmutableComponent, TransmutableBoundUserInterfaceMessage>(OnBoundUserInterfaceMessage);
    }

    private void OnDoAfter(Entity<TransmutableComponent> transmutable, ref TransmutableDoAfterEvent args)
    {
        TransmuteEntity(transmutable, args.Prototype);
    }

    private void OnBoundUserInterfaceMessage(Entity<TransmutableComponent> transmutable, ref TransmutableBoundUserInterfaceMessage args)
    {
        var user = GetEntity(args.User);
        if (!transmutable.Comp.AvailablePrototypes.Contains(args.Prototype.Id) ||
            !_prototype.HasIndex(args.Prototype) ||
            !_interaction.InRangeAndAccessible(user, transmutable.Owner))
            return;

        StartTransmuteEntity(transmutable, args.Prototype, user);
    }

    /// <summary>
    /// Start transmuting a transmutable entity into another entity, respecting delay. Plays sound if applicable.
    /// </summary>
    private void StartTransmuteEntity(Entity<TransmutableComponent> transmutable, EntProtoId prototype, EntityUid user)
    {
        var comp = transmutable.Comp;

        // Not working for some reason but I can't be assed.
        if (comp.PlayTransmuteSound)
            _audio.PlayPredicted(comp.TransmuteSound, transmutable, user);

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
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// Transmutes the given entity into a new entity of the given prototype. Transfers components from the original to the new entity based on the TransmutableComponent whitelist.
    /// </summary>
    private void TransmuteEntity(Entity<TransmutableComponent> transmutable, EntProtoId prototype)
    {
        var componentsToKeep = transmutable.Comp.KeepComponents;
        ValueList<IComponent> keptComponents = new();
        if (componentsToKeep != null)
        {
            keptComponents.EnsureCapacity(componentsToKeep.Length);
            foreach (var componentName in componentsToKeep)
            {
                if (!_entity.ComponentFactory.TryGetRegistration(componentName, out var componentRegistration) ||
                    !_entity.TryGetComponent(transmutable, componentRegistration, out var component))
                    continue;

                keptComponents.Add(_serialization.CreateCopy(component, notNullableOverride: true));
            }
        }

        var newEntity = ReplaceEntity(transmutable.Owner, prototype);

        foreach (var component in keptComponents) {
            _entity.AddComponent(newEntity, component, overwrite: true);
        }
    }

    /// <summary>
    /// Replaces an entity with a new entity in place. Keeps the mind attached to the new entity.
    /// </summary>
    private EntityUid ReplaceEntity(EntityUid oldEntity, EntProtoId prototype, bool transferInventory = true, bool transferMind = true)
    {
        var xForm = Transform(oldEntity);

        EntityUid newEntity;
        if (_container.TryGetContainingContainer(oldEntity, out var container))
        {
            _container.Remove(oldEntity, container);
            newEntity = PredictedSpawnInContainerOrDrop(prototype, container.Owner, container.ID);
        }
        else
        {
            newEntity = PredictedSpawnAttachedTo(prototype, xForm.Coordinates);
        }

        if (transferMind && _mind.TryGetMind(oldEntity, out var mindId, out var mindComp))
            _mind.TransferTo(mindId, newEntity, mind: mindComp);

        _entity.PredictedDeleteEntity(oldEntity);

        return newEntity;
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
