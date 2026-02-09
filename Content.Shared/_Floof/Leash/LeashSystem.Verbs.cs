using Content.Shared._Floof.Leash.Components;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    public static readonly VerbCategory LeashLengthConfigurationCategory =
        new("verb-categories-leash-config", "/Textures/_Floof/Interface/VerbIcons/resize.svg.192dpi.png");

    private void InitializeVerbs()
    {
        SubscribeLocalEvent<LeashedComponent, GetVerbsEvent<InteractionVerb>>(OnGetLeashedVerbs);
        SubscribeLocalEvent<LeashComponent, GetVerbsEvent<AlternativeVerb>>(OnGetLeashVerbs);
        SubscribeLocalEvent<LeashComponent, ExaminedEvent>(OnLeashExamined);

        SubscribeLocalEvent<LeashAnchorComponent, LeashAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<LeashedComponent, LeashDetachDoAfterEvent>(OnDetachDoAfter);
    }

    private void OnGetLeashedVerbs(Entity<LeashedComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || GetEntity(ent.Comp.Leash) is not { } leash
            || !TryComp<LeashComponent>(leash, out var leashComp))
            return;

        var user = args.User;
        args.Verbs.Add(new()
        {
            Text = Loc.GetString("verb-unleash-text"),
            Act = () => TryUnleash(ent.Owner, (leash, leashComp), user)
        });
    }

    private void OnGetLeashVerbs(Entity<LeashComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || ent.Comp.LengthConfigs is not { } configurations
            || !CanInteractWithLeash(args.User, ent))
            return;

        // Add a menu listing each length configuration.
        foreach (var length in configurations)
        {
            args.Verbs.Add(new()
            {
                Text = Loc.GetString("verb-leash-set-length-text", ("length", length)),
                Act = () => SetLeashLength(ent, length),
                Category = LeashLengthConfigurationCategory
            });
        }
    }

    private void OnLeashExamined(Entity<LeashComponent> ent, ref ExaminedEvent args)
    {
        var length = ent.Comp.Length;
        args.PushMarkup(Loc.GetString("leash-length-examine-text", ("length", length)));
    }

    private void OnAttachDoAfter(Entity<LeashAnchorComponent> ent, ref LeashAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled
            || !TryComp<LeashComponent>(args.Used, out var leash)
            || !CanLeash(ent, (args.Used.Value, leash)))
            return;

        DoLeash(ent, (args.Used.Value, leash), EntityUid.Invalid);
    }

    private void OnDetachDoAfter(Entity<LeashedComponent> ent, ref LeashDetachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || GetEntity(ent.Comp.Leash) is not { } leash)
            return;

        RemoveLeash(ent!, leash);
    }
}
