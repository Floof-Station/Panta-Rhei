using Content.Shared.GameTicking;
using Content.Shared.Body;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Euphoria.Surgery;

public partial class SharedSurgerySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<SurgeryComponent, OrganInsertedIntoEvent>(OnOrganInserted);
        SubscribeLocalEvent<SurgeryComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        SubscribeLocalEvent<SurgeryComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
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

    private void OnGetInteractionVerbs(EntityUid uid, SurgeryComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!TryComp<SurgeryToolComponent>(args.Using, out var usingSurgeryComp))
            return;

        var disabled = false;
        string? message = null;

        EntityUid surgeryObject = default;
        if (usingSurgeryComp != null)
            surgeryObject = args.Using!.Value;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                //if (!disabled)
                //    TryStartSurgeryDoafter(surgeryObject, args.Target, args.User);
            },
            Message = message,
            Disabled = disabled,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Text = Loc.GetString("surgery-verb-name"),
        };

        args.Verbs.Add(verb);
    }
}
