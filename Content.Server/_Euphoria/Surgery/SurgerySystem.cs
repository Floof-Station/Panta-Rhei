using System.Linq;
using Content.Shared._DV.Surgery;
using Content.Shared._Euphoria.Surgery;
using Content.Shared._Euphoria.Surgery.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Euphoria.Surgery;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
        var tool = ev.Tool;

        if (!TryComp<SurgeryToolComponent>(tool, out var comp))
        {
            if (comp == null)
                return;
        }

        _audio.PlayPvs(comp.EndSound, ev.User);

        var bodyParts = proto.getTargets();

        double bleederRoll;

        foreach(var bodyPart in bodyParts)
        {
            bleederRoll = _random.NextDouble();

            if (bleederRoll < component.ChanceOfBleeder)
            {
                if (!TryComp<BodyComponent>(uid, out var bodyComponent))
                    return;
                if (bodyComponent == null)
                    return;

                if(!_proto.TryIndex<OrganCategoryPrototype>(bodyPart, out var orgProto))
                    return;

                var orgEnt = GetTarget(bodyComponent, orgProto);

                if (orgEnt == null)
                    return;

                string bleedingId = "bleeding";

                if(!_proto.TryIndex<SurgeryStatePrototype>(bleedingId, out var state))
                    return;

                //RAISE AN EVENT TO START BLEEDING
                //FIND SOME WAY TO KEEP RAISING IT IF NOT FIXED
                component.DictOrgans[bodyPart][orgEnt.Value][state] = true;
            }
        }

        //This is where damage from cross contamination would happen

        //allows for cross contamination
        comp.LastUsedOn = uid;

        if (!ev.Cancelled)
        {
            PerformOperation(uid, component, proto, ev.User);
        }

        RefreshInterface(uid,component);
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

        //Duplicate from OnOrganRemoved -> consider taking this logic and making a function for this
        if (TryComp<SurgeryOrganDamageComponent>(args.Organ, out var damageContainer))
        {
            if (TryComp<DamageableComponent>(ent, out var damageable) &&
                _damage.GetAllDamage((ent.Owner, damageable)).DamageDict.TryGetValue(damageContainer.damageType, out var transferDamage))
            {
                if(!_proto.TryIndex(damageContainer.damageType, out var damageProto))
                    return;
                if (damageProto == null)
                    return;

                var tmp = damageContainer.Damage;
                damageContainer.Damage = 0;
                _damage.TryChangeDamage(ent.Owner, new DamageSpecifier(damageProto, tmp));
            }
        }

    }

    //This is where you would put the damage transfer from the body onto the organ
    private void OnOrganRemoved(Entity<SurgeryComponent> ent, ref OrganRemovedFromEvent args)
    {
        if(!TryComp<OrganComponent>(args.Organ, out var organ))
            return;
        if(organ.Category == null)
            return;

        if (TryComp<SurgeryOrganDamageComponent>(args.Organ, out var damageContainer))
        {
            if (TryComp<DamageableComponent>(ent, out var damageable) &&
                _damage.GetAllDamage((ent.Owner, damageable)).DamageDict.TryGetValue(damageContainer.damageType, out var transferDamage))
            {
                if(!_proto.TryIndex(damageContainer.damageType, out var damageProto))
                    return;
                if (damageProto == null)
                    return;

                damageContainer.Damage = transferDamage;
                _damage.SetDamage(ent.Owner, new DamageSpecifier(damageProto, 0));
            }
        }

        ent.Comp.DictOrgans[organ.Category.Value].Remove(args.Organ);
    }

    public List<ProtoId<SurgeryActionPrototype>> GetAvailableSurgeries(EntityUid uid, SurgeryComponent component, bool getUnavailable = false)
    {
        var filteredList = new List<ProtoId<SurgeryActionPrototype>>();

        //Add filter system here to only give surgeries that are available at the time.
        var ev = new SurgeryGetActionsEvent((uid, component), getUnavailable);
        var surgeries = getSurgeries(component);
        if (surgeries != null)
            AddSurgeries(ev.Surgeries, surgeries);
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
                        {
                            addThis = false;
                            continue;
                        }
                        //Don't add the action to remove an organ if they don't have that organ
                        else if (component.DictOrgans[action.Remove.Value].Count == 0)
                        {
                            addThis = false;
                            continue;
                        }
                    }
                    if (action.Insert != null)
                    {
                        //only allow one organ of each type to be inserted (FOR NOW)
                        if (component.DictOrgans.ContainsKey(action.Insert.Value))
                            if (component.DictOrgans[action.Insert.Value].Count > 0)
                            {
                                addThis = false;
                                continue;
                            }
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

        EntityUid? foundTool = null;
        var foundOrgan = false;

        if(proto.Insert == null)
            foundOrgan = true;

        foreach (var item in _handsSystem.EnumerateHeld(args.Actor))
        {
            if (TryComp<SurgeryToolComponent>(item, out var tool))
                if (tool.ToolType == proto._type)
                {
                    foundTool = item;
                }

            if (!foundOrgan)
            {
                if (TryComp<OrganComponent>(item, out var organ))
                    if (organ.Category == proto.Insert)
                        foundOrgan = true;
            }

        }

        if(foundTool != null && foundOrgan)
        {
            TrySurgeryStart(uid, component, args.ID, args.Actor, foundTool.Value);
        }
        if(foundTool == null)
            _popupSystem.PopupEntity(Loc.GetString("surgery-wrong-tool"),args.Actor);

        if(!foundOrgan)
            if(proto.Insert != null)
                _popupSystem.PopupEntity(Loc.GetString("surgery-no-organ",("organType",proto.Insert.Value)),args.Actor);

    }

    private bool TrySurgeryStart(EntityUid uid, SurgeryComponent component, ProtoId<SurgeryActionPrototype> id, EntityUid actor, EntityUid tool)
    {
        if (!TryComp<SurgeryToolComponent>(tool, out var toolComp))
            return false;
        if(!_proto.TryIndex<SurgeryActionPrototype>(id,out var proto))
            return false;
        if(proto == null)
            return false;

        var doFinished = new SurgeryFinishedEvent
        {
            ID = id,
            Tool = tool,
        };

        var anesthesiaScale = 1;


        //Are they under for surgery?
        if(!TryComp<AnesthesiaComponent>(uid, out var anesthesia))
            anesthesiaScale = 2;

        //Irrelevant of any other facts, the dead won't squirm
        if (_mobState.IsDead(uid))
            anesthesiaScale = 1;

        var surgerySpeed = (anesthesiaScale*proto._duration) / toolComp.Strength;

        var doAfter =
            new DoAfterArgs(EntityManager,actor,surgerySpeed,doFinished,uid, uid, tool)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
            };

        _audio.PlayPvs(toolComp.StartSound, actor);

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

    private void PerformOperation(EntityUid uid, SurgeryComponent component, SurgeryActionPrototype actionPrototype, EntityUid surgeon)
    {
        if (!TryComp<BodyComponent>(uid,out var body))
            return;
        if (body.Organs == null)
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
            EntityUid? insertEntity = null;
            //need access to the person performing the surgery to see the organ in their hand
            //Check if the organ is gone by the end of the surgery, then give message if missing
            foreach (var item in _handsSystem.EnumerateHeld(surgeon))
            {
                if (TryComp<OrganComponent>(item, out var organ))
                    if(organ.Category == actionPrototype.Insert)
                    {
                        insertEntity = item;
                        break;
                    }
            }

            if (insertEntity == null)
                _popupSystem.PopupEntity(Loc.GetString("surgery-organ-gone",("organType",actionPrototype.Insert.Value)),surgeon);
            else
            {
                ContainerSystem.Insert(insertEntity.Value, body.Organs);
            }
        }

    }

    public List<ProtoId<SurgeryActionPrototype>>? getSurgeries(SurgeryComponent surgery)
    {
        if (_proto.TryIndex(surgery.SurgeryList, out var proto))
            return proto.Actions;
        else
            return null;
    }
}
