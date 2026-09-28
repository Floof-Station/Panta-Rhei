using Content.Server.Atmos.Components;
using Content.Server.Radiation.Components;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._Floof.Vore;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared._Floof.Leash;
using Content.Shared._Floof.Leash.Components;
using Robust.Shared.Containers;
namespace Content.Server._Floof.Vore;

public sealed class PreySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedSuitSensorSystem _suitSensorSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly LeashSystem _leash = default!;
    
    private readonly HashSet<EntityUid> _pendingImmunityUpdates = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PredComponent, EntInsertedIntoContainerMessage>(OnPreyInsertedIntoContainer);
        SubscribeLocalEvent<PredComponent, EntRemovedFromContainerMessage>(OnPreyRemovedFromContainer);
        
        SubscribeLocalEvent<DevouredComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DevouredComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<DevouredComponent, MobStateChangedEvent>(OnPreyMobStateChanged);
        SubscribeLocalEvent<DevouredComponent, MoveInputEvent>(OnRelayMovement);
    }

    /// <summary>
    /// To get the most recent values for current container
    /// </summary>
    public override void Update(float frameTime){
        base.Update(frameTime);

        foreach (var uid in _pendingImmunityUpdates){
            RemoveStomachImmunities(uid);
        }

        _pendingImmunityUpdates.Clear();
    }

    /// <summary>
    /// responsible for giving the component that gives the prey immunities
    /// </summary>
    private void OnPreyInsertedIntoContainer(EntityUid uid, PredComponent comp, EntInsertedIntoContainerMessage args){
        //double check making sure its a vore_container
        if (args.Container.ID != comp.ContainerId)
            return;
        var prey = args.Entity;
        /*in case prey is leashed remove it to prevent bug that keeps pred from 
        being able to move since prey is still leashed */
        if (TryComp<LeashedComponent>(prey, out var leashed)
            && leashed.Leash is not null
            && TryGetEntity(leashed.Leash.Value, out var leash)
            && TryComp<LeashComponent>(leash.Value, out var leashComp))
        {
            _leash.RemoveLeash((prey, leashed), (leash.Value, leashComp));
        }

        EnsureComp<DevouredComponent>(prey);
    }

    /// <summary>
    /// responsible for removing components and immunities
    /// </summary>
    private void OnPreyRemovedFromContainer(EntityUid uid, PredComponent comp, EntRemovedFromContainerMessage args){
        if (TryComp<DevouredComponent>(args.Entity, out _))
            _pendingImmunityUpdates.Add(args.Entity);
    }

    private void OnStartup(EntityUid uid, DevouredComponent comp, ComponentStartup args){
        ApplyStomachImmunities(uid);
    }

    /// <summary>
    /// alternative way of escaping instead of using movement keys
    /// </summary>
    private void OnGetVerbs(EntityUid uid, DevouredComponent comp, GetVerbsEvent<Verb> args){
        if (!_containerSystem.TryGetContainingContainer(uid, out var container))
            return;
        if (args.User != args.Target)
            return;

        var pred = container.Owner;
        var prey = uid;

        args.Verbs.Add(new Verb
        {
            Text = "Struggle Free",
            Act = () => 
            {
                _popupSystem.PopupEntity("You struggle free!", prey, prey);
                _popupSystem.PopupEntity("Your prey escaped!", pred, pred);
                _containerSystem.Remove(uid, container);
            }
        });
    }

    /// <summary>
    /// in case the prey died/crit they need to be ejected from ALL vorecontainers
    /// this way a para wont accidentally stumble on a scene and the corpse wont rot
    /// </summary>
    private void OnPreyMobStateChanged(EntityUid uid, DevouredComponent comp, ref MobStateChangedEvent args){
        if (args.NewMobState != MobState.Dead && args.NewMobState != MobState.Critical)
            return;

        var safety = 0;
        while (_containerSystem.TryGetContainingContainer(uid, out var container)){
            // prevention of possible infinite loop incase failed removal (better safe than sorry)
            if (++safety > 10)
                break;
            if (!TryComp<PredComponent>(container.Owner, out var vore))
                break;
            if (container.ID != vore.ContainerId)
                break;
            if (!_containerSystem.Remove(uid, container))
                break;
        }
    }

    /// <summary>
    /// removes the ability to escape by moving when inside a vore container in order to prevent accidentally escapes
    /// </summary>
    private void OnRelayMovement(EntityUid uid, DevouredComponent  comp, ref MoveInputEvent args){
        if (!IsInVoreContainer(uid))
            return;
        args.Entity.Comp.HeldMoveButtons = default;
    }

    /// <summary>
    /// checks if an entity is inside a vore container
    /// </summary>
    /// <returns>
    /// true if the entity is inside a vore container
    /// </returns>
    private bool IsInVoreContainer(EntityUid uid){
        if (!_containerSystem.TryGetContainingContainer(uid, out var container))
            return false;

        return TryComp<PredComponent>(container.Owner, out var comp) &&
               container.ID == comp.ContainerId;
    }

    /// <summary>
    /// the prey needs to have certain components such as pressure immunity and their cords off
    /// for consent purposes -> having others avoid stumbling on scenarios
    /// </summary>
    private void ApplyStomachImmunities(EntityUid prey){
        /*double check making sure they are inside the container
        should prevent possible exploitation of the system*/
        if (!IsInVoreContainer(prey))
           return;
        if (!TryComp<DevouredComponent>(prey, out var tracker))
            return;

        if (!HasComp<PressureImmunityComponent>(prey)){
            EnsureComp<PressureImmunityComponent>(prey);
            tracker.AddedPressure = true;
        }

        if (!HasComp<TemperatureImmunityComponent>(prey)){
            EnsureComp<TemperatureImmunityComponent>(prey);
            tracker.AddedTemperature = true;
        }

        if(!HasComp<RadiationProtectionComponent>(prey)){
            EnsureComp<RadiationProtectionComponent>(prey);
            tracker.AddedRadiation = true;
        }
        if (!HasComp<FlashImmunityComponent>(prey)){
            EnsureComp<FlashImmunityComponent>(prey);
            tracker.AddedFlash = true;
        }

        var slotEnumerator = _inventorySystem.GetSlotEnumerator(prey, SlotFlags.All);
        while (slotEnumerator.NextItem(out var item, out var slot)){
            if (TryComp<SuitSensorComponent>(item, out var sensorComp)){
                tracker.OriginalSensorModes[item] = sensorComp.Mode;
                _suitSensorSystem.SetSensor((item, sensorComp), SuitSensorMode.SensorOff);
            }
        }
    }

    /// <summary>
    /// the removal of the devouredcomponent and immunities after leaving a container
    /// and the reset of their cords to their original state
    /// to avoid intentional and accidental exploitation
    /// </summary>
    private void RemoveStomachImmunities(EntityUid prey){
        // if still in a container skip alltogether for example release from multi vore
        if (IsInVoreContainer(prey))
            return;
        if (!TryComp<DevouredComponent>(prey, out var tracker))
            return;

        if (tracker.AddedPressure){
            RemComp<PressureImmunityComponent>(prey);
            tracker.AddedPressure = false;
        }
        if (tracker.AddedTemperature){
            RemComp<TemperatureImmunityComponent>(prey);
            tracker.AddedTemperature = false;
        }
        if (tracker.AddedRadiation){
            RemComp<RadiationProtectionComponent>(prey);
            tracker.AddedRadiation = false;
        }
        if (tracker.AddedFlash){
            RemComp<FlashImmunityComponent>(prey);
            tracker.AddedFlash = false;
        }
        foreach (var (item, originalMode) in tracker.OriginalSensorModes){
            if (TryComp<SuitSensorComponent>(item, out var sensorComp))
                _suitSensorSystem.SetSensor((item, sensorComp), originalMode);
        }
        tracker.OriginalSensorModes.Clear();
        RemComp<DevouredComponent>(prey);
    }
}
