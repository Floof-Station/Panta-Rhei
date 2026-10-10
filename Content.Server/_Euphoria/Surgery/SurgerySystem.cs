using System.Linq;
using Content.Server.Construction.Completions;
using Content.Shared._Euphoria.Surgery;
using Content.Shared._Euphoria.Surgery.Components;
using Content.Shared.Body;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Kitchen;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;



namespace Content.Server._Euphoria.Surgery;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        //set up the surgery window
        //InitializeUI();

        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GetVerbsEvent<InteractionVerb>>(AddVerbs);
        SubscribeLocalEvent<SurgeryComponent, OrganInsertedIntoEvent>(OnOrganInserted);
        SubscribeLocalEvent<SurgeryComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        SubscribeLocalEvent<SurgeryComponent, SurgeryStartMessage>(OnSurgeryStartMessage);
        SubscribeLocalEvent<SurgeryComponent, SurgeryFinishedEvent>(OnSurgeryFinished);
    }

    private void OnMapInit(Entity<BodyComponent> ent ,ref MapInitEvent args)
    {

    }

    private void OnSurgeryFinished(EntityUid uid, SurgeryComponent component, SurgeryFinishedEvent ev)
    {
        if(!_proto.TryIndex<SurgeryActionPrototype>(ev.ID,out var proto))
            return;
        if(proto == null)
            return;

        if (!ev.Cancelled)
        {
            PerformOperation(uid, component, proto);
            Spawn("FoodBreadPlain", uid.ToCoordinates());
        }
    }
    //This is where you would put the damage transfer from the organ into the body
    private void OnOrganInserted(Entity<SurgeryComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if(!TryComp<OrganComponent>(args.Organ, out var organ))
            return;
        if(organ.Category == null)
            return;

        if (!ent.Comp.DictOrgans.ContainsKey(organ.Category.Value))
            ent.Comp.DictOrgans[organ.Category.Value] = new Dictionary<EntityUid, Dictionary<ProtoId<SurgeryStatePrototype>, bool>>();

        ent.Comp.DictOrgans[organ.Category.Value].Add(args.Organ, new Dictionary<ProtoId<SurgeryStatePrototype>, bool>());

        foreach(var state in ent.Comp.SurgeryStates)
        {
            ent.Comp.DictOrgans[organ.Category.Value][args.Organ].Add(state,false);
        }

    }

    //This is where you would put the damage transfer from the body onto the organ
    private void OnOrganRemoved(Entity<SurgeryComponent> ent, ref OrganRemovedFromEvent args)
    {
        if(!TryComp<OrganComponent>(args.Organ, out var organ))
            return;
        if(organ.Category == null)
            return;

        ent.Comp.DictOrgans[organ.Category.Value].Remove(args.Organ);
    }

    public List<ProtoId<SurgeryActionPrototype>> GetAvailableSurgeries(EntityUid uid, SurgeryComponent component, bool getUnavailable = false)
    {
        var filteredList = new List<ProtoId<SurgeryActionPrototype>>();

        //Add filter system here to only give surgeries that are available at the time.
        var ev = new SurgeryGetActionsEvent((uid, component), getUnavailable);
        AddSurgeries(ev.Surgeries, component.SurgeryActions);
        RaiseLocalEvent(uid, ev);

        bool addThis = true;

        //This is pain, I know, I am sorry
        foreach (var protoId in ev.Surgeries)
        {
            if (_proto.TryIndex<SurgeryActionPrototype>(protoId, out var action))
            {
                addThis = true;
                if (action != null)
                {
                    if (action.Remove != null)
                    {
                        //Don't add the action to remove an organ if they don't have that organ
                        if (!component.DictOrgans.ContainsKey(action.Remove.Value))
                            continue;
                    }
                    if (action.Insert != null)
                    {
                        //only allow one organ of each type to be inserted (FOR NOW)
                        if (component.DictOrgans.ContainsKey(action.Insert.Value))
                            if (component.DictOrgans[action.Insert.Value].Count > 0)
                                continue;
                    }

                    foreach (var organ in action._states)
                    {
                        if (!component.DictOrgans.ContainsKey(organ.Key))
                        {
                            addThis = false;
                            continue;
                        }
                        foreach (var ent in component.DictOrgans[organ.Key])
                        {
                            foreach (var condition in organ.Value)
                            {
                                if (!component.DictOrgans[organ.Key][ent.Key].ContainsKey(condition.Key))
                                {
                                    addThis = false;
                                    continue;
                                }
                                if (component.DictOrgans[organ.Key][ent.Key][condition.Key] != condition.Value)
                                {
                                    addThis = false;
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            if (addThis)
                filteredList.Add(protoId);
        }

        return filteredList;
    }

    private void OnSurgeryStartMessage(EntityUid uid, SurgeryComponent component, SurgeryStartMessage args)
    {
        if (!_proto.TryIndex<SurgeryActionPrototype>(args.ID, out var proto))
            return;
        if(proto == null)
            return;

        var foundTool = false;
        var foundOrgan = false;

        if(proto.Insert == null)
            foundOrgan = true;

        foreach (var item in _handsSystem.EnumerateHeld(args.Actor))
        {
            if (TryComp<SurgeryToolComponent>(item, out var tool))
                if (tool.ToolType == proto._type)
                {
                    foundTool = true;
                }

            if (!foundOrgan)
            {
                if (TryComp<OrganComponent>(item, out var organ))
                    if (organ.Category == proto.Insert)
                        foundOrgan = true;
            }

        }

        if(foundTool && foundOrgan)
        {
            TrySurgeryStart(uid, component, args.ID, args.Actor);
        }
        if(!foundTool)
            _popupSystem.PopupEntity(Loc.GetString("surgery-wrong-tool"),args.Actor);

        if(!foundOrgan)
            if(proto.Insert != null)
                _popupSystem.PopupEntity(Loc.GetString("surgery-no-organ",("organType",proto.Insert.Value)),args.Actor);

    }

    private bool TrySurgeryStart(EntityUid uid, SurgeryComponent component, ProtoId<SurgeryActionPrototype> id, EntityUid actor)
    {
        if(!_proto.TryIndex<SurgeryActionPrototype>(id,out var proto))
            return false;
        if(proto == null)
            return false;

        var doFinished = new SurgeryFinishedEvent
        {
            ID = id,
        };

        var doAfter =
            new DoAfterArgs(EntityManager,actor,proto._duration,doFinished,uid, uid, null)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
            };
        _doAfterSystem.TryStartDoAfter(doAfter);
        return true;
    }

    private EntityUid? GetTarget(BodyComponent bodyComp, ProtoId<OrganCategoryPrototype> target)
    {

        if (bodyComp.Organs != null)
        {
            var organsToRemove = bodyComp.Organs.ContainedEntities
                .Select(p => TryComp<OrganComponent>(p, out var organ) ? (Entity<OrganComponent>?)(p, organ) : null)
                .Where(p => p != null)
                .Where(p => new ProtoId<OrganCategoryPrototype>(target) == p!.Value.Comp.Category)
                .ToList();
            //for now just remove the first one found
            //Change later to specify WHICH organ if there are multi
            return organsToRemove[0];
        }

        return null;
    }

    private void PerformOperation(EntityUid uid, SurgeryComponent component, SurgeryActionPrototype actionPrototype)
    {
        if (!TryComp<BodyComponent>(uid,out var body))
            return;

        //Set the new state of the body
        if (actionPrototype._effects.Count > 0)
        {
            foreach(var part in actionPrototype._effects)
            {
                foreach (var effect in part.Value)
                {
                    var ent = GetTarget(body, part.Key);
                    if (ent == null)
                        return;

                    component.DictOrgans[part.Key][ent.Value][effect.Key] = effect.Value;
                }

            }
        }

        //Remove organ logic
        if(actionPrototype.Remove != null)
        {
            var cutOut = GetTarget(body, actionPrototype.Remove.Value);

            if (cutOut == null || body.Organs == null)
                return;

            ContainerSystem.Remove(cutOut.Value, body.Organs);
        }

        //Insert organ logic
        if (actionPrototype.Insert != null)
        {
            //need access to the person performing the surgery to see the organ in their hand
            //Check if the organ is gone by the end of the surgery, then give message if missing
        }

    }
}
