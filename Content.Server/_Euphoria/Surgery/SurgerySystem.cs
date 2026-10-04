using System.Linq;
using Content.Server.Construction.Completions;
using Content.Shared._Euphoria.Surgery;
using Content.Shared._Euphoria.Surgery.Components;
using Content.Shared.Body;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;



namespace Content.Server._Euphoria.Surgery;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

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
    }

    private void OnMapInit(Entity<BodyComponent> ent ,ref MapInitEvent args)
    {
        if (!TryComp<BodyComponent>(ent, out var body))
            return;

        if (body.Organs == null)
            return;

        //AddComp(ent, new SurgeryComponent());

        if (!TryComp<SurgeryComponent>(ent, out var surgery))
            return;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var comp))
                continue;

            if(comp.Category == null)
                continue;

            if (!surgery.DictOrgans.ContainsKey(comp.Category.Value))
                surgery.DictOrgans[comp.Category.Value] = new List<SurgeryStatePrototype>();

            foreach(var state in surgery.SurgeryStates)
            {
                state.Organ = organ;
                surgery.DictOrgans[comp.Category.Value].Add(state);
            }

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
            ent.Comp.DictOrgans[organ.Category.Value] = new List<SurgeryStatePrototype>();

        if (!TryComp<SurgeryComponent>(ent, out var surgery) || surgery == null)
            return;

        foreach(var state in surgery.SurgeryStates)
        {
            var newState =
            surgery.DictOrgans[organ.Category.Value].Add(newState);
        }

    }

    //This is where you would put the damage transfer from the body onto the organ
    private void OnOrganRemoved(Entity<SurgeryComponent> ent, ref OrganRemovedFromEvent args)
    {
        if(!TryComp<OrganComponent>(args.Organ, out var organ))
            return;
        if(organ.Category == null)
            return;

        //let's assume one organ per type
        //YES THIS IS GARBAGE I KNOW
        if (!TryComp<SurgeryComponent>(ent, out var surgery) || surgery == null)
            return;

        foreach(var state in surgery.SurgeryStates)
        {
            var newState = (SurgeryStatePrototype)state.Clone();
            newState.Organ = args.Organ;
            surgery.DictOrgans[organ.Category.Value].Add(newState);
        }


        ent.Comp.DictOrgans[organ.Category.Value] = new List<SurgeryStatePrototype>();
    }
    public List<ProtoId<SurgeryActionPrototype>> GetAvailableSurgeries(EntityUid uid, SurgeryComponent component, bool getUnavailable = false)
    {
        var ev = new SurgeryGetActionsEvent((uid, component), getUnavailable);
        AddSurgeries(ev.Surgeries, component.SurgeryActions);
        RaiseLocalEvent(uid, ev);
        return ev.Surgeries.ToList();
    }

    private void OnSurgeryStartMessage(EntityUid uid, SurgeryComponent component, SurgeryStartMessage args)
    {
        //DO THE SURGERY WITH A DOAFTER AND REMOVE THE BITS
        Spawn("FoodBreadPlain", uid.ToCoordinates());
    }

    private void PerformOperation(EntityUid uid, SurgeryComponent component, SurgeryActionPrototype actionPrototype)
    {
        if (actionPrototype._effects.Count > 0)
        {
            foreach(var part in actionPrototype._effects)
            {
                foreach (var effect in part.Value)
                {
                    //Change the surgery component to also keep track of the status of the organs
                    //At this point you have the organ (part) and the changes (effects)
                }


            }
        }
    }

    private void RemoveOrgan(EntityUid uid, SurgeryComponent component, OrganCategoryPrototype args)
    {

    }

    private void AddOrgan(EntityUid uid, SurgeryComponent component, OrganCategoryPrototype args)
    {

    }
}
