using Content.Shared.GameTicking;
using Content.Shared.Body;

namespace Content.Shared._Euphoria.Surgery;

public sealed class SurgerySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<SurgeryComponent, OrganInsertedIntoEvent>(OnOrganInserted);
        SubscribeLocalEvent<SurgeryComponent, OrganRemovedFromEvent>(OnOrganRemoved);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<BodyComponent>(args.Mob, out var body))
            return;

        if (body.Organs == null)
            return;

        AddComp(args.Mob, new SurgeryComponent());

        if (!TryComp<SurgeryComponent>(args.Mob, out var surgery))
            return;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var comp))
                continue;

            if(comp.Category == null)
                continue;

            if (!surgery.DictOrgans.ContainsKey(comp.Category.Value))
                surgery.DictOrgans[comp.Category.Value] = new List<EntityUid>();

            surgery.DictOrgans[comp.Category.Value].Add(organ);
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
            ent.Comp.DictOrgans[organ.Category.Value] = new List<EntityUid>();

        ent.Comp.DictOrgans[organ.Category.Value].Add(args.Organ);
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
}
