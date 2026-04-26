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
using Content.Server.Atmos.Components;
using Content.Shared.Body.Events;
using Content.Shared._Common.Consent;
using Content.Shared.Verbs;
using Content.Shared.Polymorph;
using Content.Shared.Destructible;
using Robust.Shared.Configuration;
namespace Content.Server._Floof.Vore;

public sealed class VoreSystem : EntitySystem
{
    [Dependency] private readonly SharedConsentSystem _consentSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public static readonly ProtoId<ConsentTogglePrototype> isPred = "PredVore";
    public static readonly ProtoId<ConsentTogglePrototype> isPrey = "PreyVore";

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, ComponentStartup>(OnMindStartup);

        SubscribeLocalEvent<VoreComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<VoreComponent, OnVoreDoAfter>(OnVoreDoAfter);
        SubscribeLocalEvent<VoreComponent, EntRemovedFromContainerMessage>(OnVoreRemovedFromContainer);
        SubscribeLocalEvent<VoreComponent, BeingGibbedEvent>(OnGibbedRemoveContent);
        SubscribeLocalEvent<VoreComponent, DestructionEventArgs>(OnDestroyedRemoveContent);
        SubscribeLocalEvent<VoreComponent, PolymorphedEvent>(OnPolymorphedTransferContent);
    }

    /// <summary>
    /// gives the vorecomponent to every entity that has the mindcomponent
    /// in order to avoid giving every mob it one by one
    /// </summary>
    private void OnMindStartup(EntityUid uid, MindContainerComponent comp, ComponentStartup args){
        EnsureComp<VoreComponent>(uid);
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
            var container = _containerSystem.EnsureContainer<Container>(target, "vore_container");
            if (container.ContainedEntities.Count > 0){
                args.Verbs.Add(new Verb
                {
                    Text = "Remove Prey",
                    Act = () => OnTryReleasePrey(target)
                });
            }
            return;
        }

        // only when reachable & interactable
        if (!args.CanInteract || !args.CanAccess)
            return;

        // to avoid feeding yourself to items
        if (!HasComp<BodyComponent>(target))
            return;
        
        // to avoid empty mind NPCs
        if (!TryComp<MindContainerComponent>(target, out var mindContainer) || mindContainer.Mind == null)
            return;

        //no verbs for swallowed people
        /**TODO
        making multivore possible. As of now its just a prevention method to avoid giving 
        Components such as space immunity to the pred
        */
        if (_containerSystem.TryGetContainingContainer(user, out var userContainer) && userContainer.ID == "vore_container")
            return;

        // devour (pred → prey)
        if (_consentSystem.HasConsent(user, isPred)
            && _consentSystem.HasConsent(target, isPrey)){
            args.Verbs.Add(new Verb
            {
                Text = "Devour",
                Act = () => OnTryVore(user, target)
            });
        }

        // insert self (prey → pred)
        if (_consentSystem.HasConsent(user, isPrey)
            && _consentSystem.HasConsent(target, isPred)){
            args.Verbs.Add(new Verb
            {
                Text = "Insert Self",
                Act = () => OnTryVore(target, user)
            });
        }
    }
        
    /// <summary>
    /// used for after selecting to insert into someone or devour
    /// will create a slow popup and warning to give both sides time to react on it
    /// </summary>
    private void OnTryVore(EntityUid user, EntityUid target){

        //slow loading bar to avoid instant vore with warning pop ups
        var doAfterArgs = new DoAfterArgs(EntityManager, user, 5f, new OnVoreDoAfter(), user, target: target, used: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,      
        };
        if (!_doAfterSystem.TryStartDoAfter(doAfterArgs))
            return;
        _popupSystem.PopupEntity($"You are devouring someone!", user, user);
        _popupSystem.PopupEntity($"You are being devoured!", target, target);
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

        var container = _containerSystem.EnsureContainer<Container>(args.User, "vore_container");
        
        //as a way to prevent too many entities to be devoured
        if (container.ContainedEntities.Count > args.MaxPrey){
            _popupSystem.PopupEntity("You are too full to swallow more prey.", args.User, args.User);
            return;
        }

        //moves prey inside the person
        _containerSystem.Insert(prey, container);
        
        /*make the prey immune to space+temp+breathing to avoid consent concerns from outside influence
        gets removed after escaping or being forcefully ejected by pred*/
        ApplyStomachImmunities(prey);
    }

    /// <summary>
    /// for when the pred removes the prey from their container
    /// will remove the buffs such as space immunity for the target
    /// </summary>
    private void OnTryReleasePrey(EntityUid pred){
        var container = _containerSystem.EnsureContainer<Container>(pred, "vore_container");
        var preyList = new List<EntityUid>(container.ContainedEntities);
        //remove everything from people to items
        foreach (var prey in preyList){
            // in case pred intentionally releases the prey to avoid escape popups
            if (TryComp<VoreComponent>(prey, out var preyComp))
                preyComp.IntentionalRelease = true;

            _containerSystem.Remove(prey, container);
            RemoveStomachImmunities(prey);
            _popupSystem.PopupEntity("You have been released!", prey, prey);
        }
        _popupSystem.PopupEntity("You release your prey.", pred, pred);
    }

    /// <summary>
    /// in case the prey chose to escape themself
    /// will remove the buffs such as space immunity for the target
    /// </summary>
    private void OnVoreRemovedFromContainer(EntityUid uid, VoreComponent comp, EntRemovedFromContainerMessage args){
        if (args.Container.ID != "vore_container")
            return;

        var prey = args.Entity;

        // Check if this was an intentional release by the pred (not a self-escape)
        if (TryComp<VoreComponent>(prey, out var preyComp) && preyComp.IntentionalRelease){
            preyComp.IntentionalRelease = false;
            return;
        }

        RemoveStomachImmunities(prey);
        _popupSystem.PopupEntity("You struggle free!", prey, prey);
        _popupSystem.PopupEntity("Your prey escaped!", uid, uid);
    }

    /// <summary>
    /// in case the user gets gibbed need content emptied including prey+items
    /// </summary>
    private void OnGibbedRemoveContent(EntityUid uid, VoreComponent comp, BeingGibbedEvent args){
        OnTryReleasePrey(uid);
    }

    /// <summary>
    /// in case the user gets destroyed through for example singulo or gibbing
    /// </summary>
    private void OnDestroyedRemoveContent(EntityUid uid, VoreComponent comp, DestructionEventArgs args){
        OnTryReleasePrey(uid);
    }

    /// <summary>
    /// in case of polymorp scenarios such as kitsune release all the content
    /// </summary>
    private void OnPolymorphedTransferContent(EntityUid uid, VoreComponent comp, PolymorphedEvent args){   
        OnTryReleasePrey(uid);
    }
    
    /// <summary>
    /// the prey needs to have certain components such as pressure immunity
    /// for consent purposes -> having others avoid stumbling on scenarios
    /// </summary>
    private void ApplyStomachImmunities(EntityUid prey){
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
    }

    /// <summary>
    /// the removal of the components after leaving a container
    /// to avoid intentional and accidental exploitation
    /// </summary>
    private void RemoveStomachImmunities(EntityUid prey){
        if (!TryComp<VoreImmunityTrackerComponent>(prey, out var tracker))
            return;
        if (tracker.AddedPressure)
            RemComp<PressureImmunityComponent>(prey);
        if (tracker.AddedBreathing)
            RemComp<BreathingImmunityComponent>(prey);
        if (tracker.AddedTemperature)
            RemComp<TemperatureImmunityComponent>(prey);
        RemComp<VoreImmunityTrackerComponent>(prey);
    }
}