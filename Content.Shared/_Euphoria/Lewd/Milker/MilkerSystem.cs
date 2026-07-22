using Content.Shared.DragDrop;
using Content.Shared.Whitelist;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Chemistry.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Dataset;
using Content.Shared.Destructible;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Audio;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
using Content.Shared._Floof.Leash.Components;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Shared._Floof.Lewd.Milker;

[Serializable]
public sealed class MilkerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MilkerComponent, ComponentStartup>(OnComponentInit);
        SubscribeLocalEvent<MilkerComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<MilkerComponent, DragDropTargetEvent>(OnDragDropped);
        SubscribeLocalEvent<MilkerComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<MilkerComponent, MilkerDoAfterEvent>(FinishAttaching);
        SubscribeLocalEvent<MilkerComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnComponentInit(Entity<MilkerComponent> entity, ref ComponentStartup args)
    {
        base.Initialize();
    }

    private void OnDestroyed(Entity<MilkerComponent> entity, ref DestructionEventArgs args)
    {
        Detach(entity);
    }

    private void OnCanDrop(Entity<MilkerComponent> entity, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop |= CheckInteraction(entity, args.Dragged, args.User);
    }

    private void OnDragDropped(Entity<MilkerComponent> entity, ref DragDropTargetEvent args)
    {
        args.Handled = true;
        TryAttaching(entity, args.Dragged, args.User);

    }

    private void OnInteractHand(Entity<MilkerComponent> entity, ref InteractHandEvent args)
    {
        TryAttaching(entity, args.User, args.User);
    }

    public void TryAttaching(Entity<MilkerComponent> entity, EntityUid target, EntityUid user)
    {
        if (!CheckInteraction(entity, target, user))
            return;

        if (entity.Comp.MilkedEntity != null)
        {
            Detach(entity);
        }
        else if (CheckMilkable(entity, target))
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.AttachDelay, new MilkerDoAfterEvent(), entity, target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                AttemptFrequency = AttemptFrequency.EveryTick
            };
            _popupSystem.PopupPredicted(Loc.GetString("milker-attach-start-popup",
                ("user", Identity.Entity(user, EntityManager)),
                ("target", Identity.Entity(target, EntityManager)),
                ("entity", Identity.Entity(entity, EntityManager))
            ), entity.Owner, user);
            _doAfter.TryStartDoAfter(doAfterArgs);
        }
        else
        {
            // Only shown to the person who attempted to attach them
            _popupSystem.PopupClient(Loc.GetString("milker-attach-fail-popup",
                ("target", Identity.Entity(target, EntityManager))
            ), entity.Owner, user);
        }
    }

    void FinishAttaching(Entity<MilkerComponent> entity, ref MilkerDoAfterEvent args)
    {
        if (!args.Cancelled && args.Target != null)
        {
            Attach(entity, (EntityUid)args.Target);
        }
    }

    public void Attach(Entity<MilkerComponent> entity, EntityUid target)
    {
        if (_net.IsClient)
            return;

        _popupSystem.PopupPredicted(Loc.GetString("milker-attach-finish-popup",
            ("target", Identity.Entity(target, EntityManager)),
            ("entity", Identity.Entity(entity, EntityManager))
        ), entity.Owner, target);
        _ambient.SetAmbience(entity, true);
        entity.Comp.MilkedSolution = _solution.EnumerateSolutions(target).First((solution) => entity.Comp.MilkedSolutionWhitelist.Contains(solution.Name)).Name;
        entity.Comp.MilkedEntity = target;

        if (entity.Comp.TubeSprite is { } sprite)
        {
            _container.EnsureContainer<ContainerSlot>(entity, MilkerComponent.VisualsContainerName);
            if (EntityManager.TrySpawnInContainer(null, entity, MilkerComponent.VisualsContainerName, out var visualEntity))
            {
                var visualComp = EnsureComp<LeashedVisualsComponent>(visualEntity.Value);
                visualComp.Sprite = sprite;
                visualComp.Source = entity;
                visualComp.Target = target;
            }
        }
        Dirty(entity);
    }

    public void Detach(Entity<MilkerComponent> entity)
    {
        if (_net.IsClient)
            return;

        if (entity.Comp.MilkedEntity != null)
        {
            if (Exists(entity.Comp.MilkedEntity))
            {
                _popupSystem.PopupPredicted(Loc.GetString("milker-detach-popup",
                    ("target", Identity.Entity((EntityUid)entity.Comp.MilkedEntity, EntityManager)),
                    ("entity", Identity.Entity(entity, EntityManager))
                ), entity.Owner, entity.Comp.MilkedEntity);
            }
            _ambient.SetAmbience(entity, false);
            entity.Comp.MilkedSolution = null;
            entity.Comp.MilkedEntity = null;
            if (_container.TryGetContainer(entity, MilkerComponent.VisualsContainerName, out var visualsContainer))
                _container.CleanContainer(visualsContainer);
            Dirty(entity);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MilkerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            // If not attached to anything, skip
            if (component.MilkedEntity == null)
                continue;

            // Check if it's time for our next tick
            if (_timing.CurTime < component.NextUpdate)
                continue;
            component.NextUpdate = _timing.CurTime + component.UpdateDelay;

            // If the milked entity no longer exists, detach
            if (!Exists(component.MilkedEntity))
            {
                Detach(new Entity<MilkerComponent>(uid, component));
                continue;
            }

            // If the milked entity is too far away, detach them
            if (Vector2.DistanceSquared(
                _xform.GetWorldPosition(uid),
                _xform.GetWorldPosition((EntityUid)component.MilkedEntity))
                > component.TubeLength * component.TubeLength)
            {
                Detach(new Entity<MilkerComponent>(uid, component));
                continue;
            }

            // Check if we have power (also should check if the power switch is on)
            if (!_powerReceiver.IsPowered(uid))
                continue;

            // Do the milking
            _solution.TryGetSolution(uid, component.Solution, out Entity<SolutionComponent>? solution);
            _solution.TryGetSolution((EntityUid)component.MilkedEntity, component.MilkedSolution, out Entity<SolutionComponent>? source);
            if (solution == null || source == null)
                continue;
            _solution.TryTransferSolution(
                (Entity<SolutionComponent>)solution,
                ((SolutionComponent)source).Solution,
                component.MilkedAmount);

            // Display some supportive prompts
            _prototypeManager.TryIndex<LocalizedDatasetPrototype>(component.PopupDataset, out LocalizedDatasetPrototype? dataset);
            if (dataset != null && _random.NextFloat() < component.PopupChance)
                _popupSystem.PopupClient(Loc.GetString(_random.Pick(dataset.Values)), (EntityUid)component.MilkedEntity, (EntityUid)component.MilkedEntity, PopupType.Medium);
        }
    }

    // Checks if the entity is whitelist valid - can we milk this twink?
    public bool CheckMilkable(Entity<MilkerComponent> milker, EntityUid target)
    {
        if (!_whitelist.IsWhitelistPass(milker.Comp.MilkedEntityWhitelist, target))
            return false;
        if (!_solution.EnumerateSolutions(target).Any((solution) => milker.Comp.MilkedSolutionWhitelist.Contains(solution.Name)))
            return false;
        return true;
    }

    // Checks if the entity can actually be reached by the milker and milkee
    public bool CheckInteraction(Entity<MilkerComponent> milker, EntityUid target, EntityUid user)
    {
        if (!_actionBlocker.CanComplexInteract(user))
            return false;

        if (!_interaction.InRangeUnobstructed(user, target))
            return false;
        return true;
    }

}
