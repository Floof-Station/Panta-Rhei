using Content.Shared._Euphoria.Surgery;
using Content.Shared._Euphoria.Surgery.Components;
using Content.Shared.Body;
using Content.Shared.GameTicking;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;


namespace Content.Server._Euphoria.Surgery;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        //set up the surgery window
        //InitializeUI();

        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GetVerbsEvent<InteractionVerb>>(AddVerbs);
        SubscribeLocalEvent<SurgeryComponent, OrganInsertedIntoEvent>(OnOrganInserted);
        SubscribeLocalEvent<SurgeryComponent, OrganRemovedFromEvent>(OnOrganRemoved);
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
                surgery.DictOrgans[comp.Category.Value] = new List<EntityUid>();

            surgery.DictOrgans[comp.Category.Value].Add(organ);
        }

    }

    private void AddVerbs(GetVerbsEvent<InteractionVerb> args)
    {

        if (!TryComp<SurgeryToolComponent>(args.Using, out var usingSurgeryComp))
            return;

        if (!TryComp<SurgeryComponent>(args.Target, out var surgery))
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
                if (!TryComp<ActivatableUIComponent>(args.Target, out var uiKey))
                    return;
                if (uiKey.Key == null)
                    return;

                //if (!disabled)
                //    TryStartSurgeryDoafter(surgeryObject, args.Target, args.User);
                _ui.TryOpenUi(args.Target, uiKey.Key,args.User);

            },
            Message = message,
            Disabled = disabled,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Text = Loc.GetString("surgery-verb-name"),
        };

        args.Verbs.Add(verb);
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
