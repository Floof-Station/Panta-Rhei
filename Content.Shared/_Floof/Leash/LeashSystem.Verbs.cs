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
        SubscribeLocalEvent<LeashedComponent, GetVerbsEvent<InnateVerb>>(OnGetLeashedVerbs);
        SubscribeLocalEvent<LeashComponent, GetVerbsEvent<AlternativeVerb>>(OnGetLeashVerbs);
        SubscribeLocalEvent<LeashAnchorComponent, GetVerbsEvent<EquipmentVerb>>(OnGetEquipmentVerbs);
        SubscribeLocalEvent<LeashComponent, ExaminedEvent>(OnLeashExamined);

        SubscribeLocalEvent<LeashAnchorComponent, LeashAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<LeashedComponent, LeashDetachDoAfterEvent>(OnDetachDoAfter);
    }

    private void OnGetLeashedVerbs(Entity<LeashedComponent> ent, ref GetVerbsEvent<InnateVerb> args)
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
            Act = () => TryStartUnleashing(ent.Owner, (leash, leashComp), user)
        });
    }

    private void OnGetLeashVerbs(Entity<LeashComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess
            || !args.CanInteract
            || ent.Comp.AvailableConfigs is not { } configurations
            || !CanInteractWithLeash(args.User, ent))
            return;

        // Add a menu listing each length configuration.
        foreach (var config in configurations)
        {
            if (!_protoMan.TryIndex(config.RopeConfig, out var configProto))
                return;

            var length = config.Length;
            var links = configProto.Links;

            args.Verbs.Add(new()
            {
                Text = Loc.GetString("verb-leash-set-length-text", ("length", length), ("links", links)),
                Act = () => SetLeashConfig(ent, config),
                Category = LeashLengthConfigurationCategory
            });
        }
    }

    private void OnGetEquipmentVerbs(Entity<LeashAnchorComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanInteract
            || !TryGetLeashTarget(ent!, out var leashTarget)
            || !_interaction.InRangeUnobstructed(args.User, leashTarget) // Can't use CanAccess here since clothing
            || args.Using is not { } leash
            || !TryComp<LeashComponent>(leash, out var leashComp))
            return;

        var user = args.User;
        var leashVerb = new EquipmentVerb { Text = Loc.GetString("verb-leash-text") };

        if (CanLeash(ent, (leash, leashComp)))
            leashVerb.Act = () => TryStartLeashing(ent, (leash, leashComp), user);
        else
        {
            leashVerb.Message = Loc.GetString("verb-leash-error-message");
            leashVerb.Disabled = true;
        }

        args.Verbs.Add(leashVerb);


        if (!TryComp<LeashedComponent>(leashTarget, out var leashedComp)
            || leashedComp.Leash != GetNetEntity(leash)
            || HasComp<LeashedComponent>(ent)) // This one means that OnGetLeashedVerbs will add a verb to remove it
            return;

        var unleashVerb = new EquipmentVerb
        {
            Text = Loc.GetString("verb-unleash-text"),
            Act = () => TryStartUnleashing((leashTarget, leashedComp), (leash, leashComp), user)
        };
        args.Verbs.Add(unleashVerb);
    }

    private void OnLeashExamined(Entity<LeashComponent> ent, ref ExaminedEvent args)
    {
        var config = ent.Comp.CurrentConfig;
        if (!_protoMan.TryIndex(config.RopeConfig, out var configProto))
            return;

        var length = config.Length;
        var links = configProto.Links;
        args.PushMarkup(Loc.GetString("leash-length-examine-text", ("length", length), ("links", links)));
    }

    private void OnAttachDoAfter(Entity<LeashAnchorComponent> ent, ref LeashAttachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled
            || !TryComp<LeashComponent>(args.Used, out var leash)
            || !CanLeash(ent, (args.Used.Value, leash)))
            return;

        TryLeash(ent, (args.Used.Value, leash), EntityUid.Invalid);
    }

    private void OnDetachDoAfter(Entity<LeashedComponent> ent, ref LeashDetachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || GetEntity(ent.Comp.Leash) is not { } leash)
            return;

        RemoveLeash(ent!, leash);
    }
}
