using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Content.Shared.Body.Components;
using Content.Shared.Mind.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.FloofStation;
using Content.Shared._Floof.Vore;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared._DV.CosmicCult.Components;
using Content.Server.Radiation.Components;
using Content.Server.Atmos.Components;
using Content.Shared.Body.Events;
using Content.Shared._Common.Consent;
using Content.Shared.Verbs;
using Content.Shared.Polymorph;
using Content.Shared.Destructible;
using Robust.Shared.Configuration;
using Content.Shared._DV.Carrying;
using Robust.Shared.Timing;
namespace Content.Server._Floof.Vore;

public sealed class VoreSystem : EntitySystem
{
    [Dependency] private readonly SharedConsentSystem _consentSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly CarryingSystem _carryingSystem = default!;

    public static readonly ProtoId<ConsentTogglePrototype> isPred = "PredVore";
    public static readonly ProtoId<ConsentTogglePrototype> isPrey = "PreyVore";

    private readonly HashSet<EntityUid> _pendingConsentUpdates = new();
    private readonly HashSet<EntityUid> _pendingImmunityUpdates = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsentComponent, ComponentStartup>(OnConsentStartup);
        SubscribeLocalEvent<ConsentComponent, EntityConsentToggleUpdatedEvent>(OnConsentUpdated);

        SubscribeLocalEvent<VoreComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<VoreComponent, OnVoreDoAfter>(OnVoreDoAfter);
        SubscribeLocalEvent<VoreComponent, EntRemovedFromContainerMessage>(OnVoreRemovedFromContainer);
        SubscribeLocalEvent<VoreComponent, BeingGibbedEvent>(OnGibbedRemoveContent);
        SubscribeLocalEvent<VoreComponent, DestructionEventArgs>(OnDestroyedRemoveContent);
        SubscribeLocalEvent<VoreComponent, PolymorphedEvent>(OnPolymorphedTransferContent);
    }

    /// <summary>
    /// To get the most recent values for consent and current container
    /// </summary>
    public override void Update(float frameTime){
        base.Update(frameTime);

        // processing of consent updates
        foreach (var uid in _pendingConsentUpdates){
            ApplyVoreConsent(uid);
        }
        _pendingConsentUpdates.Clear();

        // processing of immunity updates
        foreach (var uid in _pendingImmunityUpdates){
            RemoveStomachImmunities(uid);
        }
        _pendingImmunityUpdates.Clear();
    }

    /// <summary>
    /// gives the mob vore component when they updated their consent to be pred or prey
    /// in order to avoid giving every mob it one by one, timer needed to get the recent change 
    /// </summary>
    private void OnConsentUpdated(EntityUid uid, ConsentComponent comp, EntityConsentToggleUpdatedEvent args){
        // only if the updated toggle is prey or pred
        if (args.ConsentToggleProtoId != isPred && args.ConsentToggleProtoId != isPrey)
            return;
        _pendingConsentUpdates.Add(uid);
    }

    /// <summary>
    /// same principle as OnConsentUpdated but without the need for checking consent change
    /// </summary>
    private void OnConsentStartup(EntityUid uid, ConsentComponent comp, ComponentStartup args){
        _pendingConsentUpdates.Add(uid);
    }

    /// <summary>
    /// gives a mob the vore component if they have selected either pred or prey consent and removes it if they have neither
    /// </summary>
    private void ApplyVoreConsent(EntityUid uid){
        var hasPred = _consentSystem.HasConsent(uid, isPred);
        var hasPrey = _consentSystem.HasConsent(uid, isPrey);
        //TODO var for digest
        
        /* in case prey is inside a container immediately release them when they turn off prey consent
        works as an emergency leave for the prey*/
        if (!hasPrey &&
        TryComp<VoreComponent>(uid, out var comp) &&
        IsInVoreContainer(uid, comp) &&
        _containerSystem.TryGetContainingContainer(uid, out var container)){
            _containerSystem.Remove(uid, container);
        }

        //give the mob the needed component to be able to see the verbs
        if (hasPred || hasPrey){
            // to avoid item ghostroles like trays getting vore components
            if (HasComp<BodyComponent>(uid))
                EnsureComp<VoreComponent>(uid);
        }
        else{
            RemComp<VoreComponent>(uid);
        }
        
        //TODO component for digest
    }

    /// <summary>
    /// creates verbs inside the interaction menu for yourself and other mobs controlled by players 
    /// only show up when the consent has been selected on both sides
    /// </summary>
    private void OnGetVerbs(EntityUid uid, VoreComponent comp, GetVerbsEvent<Verb> args){
        var user = args.User;
        var target = args.Target;

        // using command to turn on/off verb components
        if (!_cfg.GetCVar(VoreCVars.VoreEnabled))
            return;
        
        // no self activation, only there to remove your own prey and not have other intervene or have others see that you have prey
        if (user == target){
            var container = _containerSystem.EnsureContainer<Container>(target, comp.ContainerId);
            if (container.ContainedEntities.Count > 0){
                args.Verbs.Add(new Verb
                {
                    Text = "Remove Prey",
                    Act = () =>TryReleasePrey(target, comp)
                });
            }
            return;
        }

        // only when reachable & interactable
        if (!args.CanInteract || !args.CanAccess)
            return;
        
        // to avoid empty mind NPCs
        if (!TryComp<MindContainerComponent>(target, out var mindContainer) || mindContainer.Mind == null)
            return;

        /* if the user is a pred inside a pred allows them to have interactions with prey inside
        only if they are in the same container however (not just same type but literally)*/ 
        if (!IsValidVoreInteraction(user, target, comp))
            return;

        // devour (pred → prey)
        if (_consentSystem.HasConsent(user, isPred)
            && _consentSystem.HasConsent(target, isPrey)){
            args.Verbs.Add(new Verb
            {
                Text = "Devour",
                Act = () => TryVore(user, target)
            });
        }
        // insert self (prey → pred)
        if (_consentSystem.HasConsent(user, isPrey)
            && _consentSystem.HasConsent(target, isPred)){
            args.Verbs.Add(new Verb
            {
                Text = "Insert Self",
                Act = () => TryVore(target, user)
            });
        }
    }

    /// <summary>
    /// used for after selecting to insert into someone or devour
    /// will create a slow popup and warning to give both sides time to react on it
    /// </summary>
    private void TryVore(EntityUid user, EntityUid target){

        //slow loading bar to avoid instant vore with warning pop ups
        var doAfterArgs = new DoAfterArgs(EntityManager, user, 5f, new OnVoreDoAfter(), user, target: target, used: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,      
        };
        if (!_doAfterSystem.TryStartDoAfter(doAfterArgs))
            return;
        _popupSystem.PopupEntity($"You are devouring someone!", user, user);
        _popupSystem.PopupEntity($"You are being devoured!", target, target, PopupType.LargeCaution);
    }

    /// <summary>
    /// moving the player inside the artificial storage
    /// will also give buffs such as space immunity for the target
    /// </summary>
    private void OnVoreDoAfter(EntityUid uid, VoreComponent comp, OnVoreDoAfter args){
        //handles canceled events
        if (args.Cancelled || args.Handled)
            return;
        if (args.Target is not EntityUid prey)
            return;

        var pred = uid;
        var container = _containerSystem.EnsureContainer<Container>(pred, comp.ContainerId);
        
        var count = 0;
        //only counts entities with bodies meaning no items
        foreach (var e in container.ContainedEntities){
            if (HasComp<BodyComponent>(e))
                count++;
        }
        //as a way to prevent too many entities to be devoured
        if (count >= args.MaxPrey){
            _popupSystem.PopupEntity("You are too full to swallow more prey.", pred, pred);
            return;
        }

        //makes sure prey will be dropped from bags and hands
        EnsureEntityFree(pred, prey, comp);

        //moves prey inside the person
        _containerSystem.Insert(prey, container);
        
        /*make the prey immune to space+temp+breathing to avoid consent concerns from outside influence
        gets removed after escaping or being forcefully ejected by pred*/
        ApplyStomachImmunities(prey, comp);
    }

    /// <summary>
    /// makes sure the prey is not inside any other container such as 
    /// bags or being carried by someone before being inserted into the pred
    /// </summary>
    private void EnsureEntityFree(EntityUid pred, EntityUid prey, VoreComponent comp){
         //check if the prey is already inside a container and remove them (for example bags)
        if (_containerSystem.TryGetContainingContainer(prey, out var currentContainer)){
            if (currentContainer.ID != comp.ContainerId)
                _containerSystem.Remove(prey, currentContainer);
        }

        //in case prey is being carried by pred, someone else or is holding the prey drop them
        // 1. pred carrying prey
        if (TryComp<CarryingComponent>(pred, out var predCarrying) &&
            predCarrying.Carried == prey)
            _carryingSystem.DropCarried(pred, prey);
        // 2. prey carrying pred
        if (TryComp<CarryingComponent>(prey, out var preyCarrying) &&
        preyCarrying.Carried == pred)
            _carryingSystem.DropCarried(prey, pred);
        // 3. prey being carried by someone else
        if (TryComp<BeingCarriedComponent>(prey, out var preyBeingCarried) &&
        preyBeingCarried.Carrier != pred)
            _carryingSystem.DropCarried(preyBeingCarried.Carrier, prey);
    }

    /// <summary>
    /// for when the pred removes the prey from their container
    /// will remove the buffs such as space immunity for the target
    /// </summary>
    private void TryReleasePrey(EntityUid pred, VoreComponent comp){
        var container = _containerSystem.EnsureContainer<Container>(pred, comp.ContainerId);
        var preyList = new List<EntityUid>(container.ContainedEntities);
        //remove everything from people to items
        foreach (var prey in preyList){
            // in case pred intentionally releases the prey to avoid escape popups
            if (TryComp<VoreComponent>(prey, out var preyComp))
                preyComp.IntentionalRelease = true;

            _containerSystem.Remove(prey, container);
            _pendingImmunityUpdates.Add(prey);
            _popupSystem.PopupEntity("You have been released!", prey, prey);
        }
        _popupSystem.PopupEntity("You release your prey.", pred, pred);
    }

    /// <summary>
    /// in case the prey chose to escape themself
    /// will remove the buffs such as space immunity for the target
    /// </summary>
    private void OnVoreRemovedFromContainer(EntityUid uid, VoreComponent comp, EntRemovedFromContainerMessage args){
        if (args.Container.ID != comp.ContainerId)
            return;

        var prey = args.Entity;

        // Check if this was an intentional release by the pred (not a self-escape)
        if (TryComp<VoreComponent>(prey, out var preyComp) && preyComp.IntentionalRelease){
            preyComp.IntentionalRelease = false;
            return;
        }

        _pendingImmunityUpdates.Add(prey);
        _popupSystem.PopupEntity("You struggle free!", prey, prey);
        _popupSystem.PopupEntity("Your prey escaped!", uid, uid);
    }

    /// <summary>
    /// in case the user gets gibbed need content emptied including prey+items
    /// </summary>
    private void OnGibbedRemoveContent(EntityUid uid, VoreComponent comp, BeingGibbedEvent args){
        TryReleasePrey(uid, comp);
    }

    /// <summary>
    /// in case the user gets destroyed through for example singulo or gibbing
    /// </summary>
    private void OnDestroyedRemoveContent(EntityUid uid, VoreComponent comp, DestructionEventArgs args){
        TryReleasePrey(uid, comp);
    }

    /// <summary>
    /// in case of polymorp scenarios such as kitsune release all the content
    /// </summary>
    private void OnPolymorphedTransferContent(EntityUid uid, VoreComponent comp, PolymorphedEvent args){   
        TryReleasePrey(uid, comp);
    }
    
    /// <summary>
    /// the prey needs to have certain components such as pressure immunity
    /// for consent purposes -> having others avoid stumbling on scenarios
    /// </summary>
    private void ApplyStomachImmunities(EntityUid prey, VoreComponent comp){
        /*double check making sure they are inside the container
        should prevent possible exploitation of the system*/
        if (!IsInVoreContainer(prey, comp))
           return;

        var tracker = EnsureComp<VoreImmunityTrackerComponent>(prey);
        if (!HasComp<PressureImmunityComponent>(prey))
        {
            EnsureComp<PressureImmunityComponent>(prey);
            tracker.AddedPressure = true;
        }

        if (!HasComp<BreathingImmunityComponent>(prey))
        {
            EnsureComp<BreathingImmunityComponent>(prey);
            tracker.AddedBreathing = true;
        }

        if (!HasComp<TemperatureImmunityComponent>(prey))
        {
            EnsureComp<TemperatureImmunityComponent>(prey);
            tracker.AddedTemperature = true;
        }
        /* doesnt fully protect from radiation (given its potassium iodine protection meaning 90 percent reduction of radiation damage) 
        but will give prey more time to react and escape before radiation starts doing damage */
        if (!HasComp<RadiationProtectionComponent>(prey))
        {
            EnsureComp<RadiationProtectionComponent>(prey);
            tracker.AddedRadiation = true;
        }
    }

    /// <summary>
    /// the removal of the components after leaving a container
    /// to avoid intentional and accidental exploitation
    /// </summary>
    private void RemoveStomachImmunities(EntityUid prey){
        if (!TryComp<VoreImmunityTrackerComponent>(prey, out var tracker))
            return;
        // if still in a container skip alltogether for example release from multi vore
        if (TryComp<VoreComponent>(prey, out var comp) && IsInVoreContainer(prey, comp))
            return;

        if (tracker.AddedPressure)
            RemComp<PressureImmunityComponent>(prey);
        if (tracker.AddedBreathing)
            RemComp<BreathingImmunityComponent>(prey);
        if (tracker.AddedTemperature)
            RemComp<TemperatureImmunityComponent>(prey);
        if (tracker.AddedRadiation)
            RemComp<RadiationProtectionComponent>(prey);
        
        RemComp<VoreImmunityTrackerComponent>(prey);
    }

    /// <summary>
    /// checks if an entity is inside a vore container
    /// </summary>
    /// <returns>
    /// true if the entity is inside any vore container, otherwise false
    /// </returns>
    private bool IsInVoreContainer(EntityUid uid, VoreComponent comp){
        return _containerSystem.TryGetContainingContainer(uid, out var container) &&
           container.ID == comp.ContainerId;
    }

    /// <summary>
    /// checks if prey is inside a vore container to only allow vore in the same container
    /// </summary>
    /// <returns>
    /// false if only one is in a vore container or if both are inside another container
    /// </returns>
    private bool IsValidVoreInteraction(EntityUid user, EntityUid target, VoreComponent comp){
        var userInVore = IsInVoreContainer(user, comp);
        var targetInVore = IsInVoreContainer(target, comp);

        // one in vore, one not → invalid
        if (userInVore != targetInVore)
            return false;

        // both in vore → must be same stomach instance
        if (userInVore)
        {
            _containerSystem.TryGetContainingContainer(user, out var userContainer);
            _containerSystem.TryGetContainingContainer(target, out var targetContainer);

            if (userContainer!.Owner != targetContainer!.Owner)
                return false;
        }

        return true;
    }
}