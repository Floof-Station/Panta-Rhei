using Content.Shared._Euphoria.Selector.Events;
using Content.Shared._Floof.Leash.Components;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    private void InitializeVerbs()
    {
        SubscribeLocalEvent<LeashComponent, BeforeConfigurationSelectedEvent>(BeforeConfigSelected);
        SubscribeLocalEvent<LeashComponent, ConfigurationSelectedEvent>(OnConfigSelected);

        SubscribeLocalEvent<LeashedComponent, GetVerbsEvent<InnateVerb>>(OnGetLeashedVerbs);
        SubscribeLocalEvent<LeashAnchorComponent, GetVerbsEvent<EquipmentVerb>>(OnGetEquipmentVerbs);

        SubscribeLocalEvent<LeashAnchorComponent, LeashAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<LeashedComponent, LeashDetachDoAfterEvent>(OnDetachDoAfter);
    }

    private void BeforeConfigSelected(Entity<LeashComponent> ent, ref BeforeConfigurationSelectedEvent args)
    {
        if (args.User != null && !CanInteractWithLeash(args.User.Value, ent))
        {
            args.Cancel(Loc.GetString("leash-config-cancel-cant-interact"));
            return;
        }
    }

    private void OnConfigSelected(Entity<LeashComponent> ent, ref ConfigurationSelectedEvent args)
    {
        if (args.Group.Id == "length")
        {
            ent.Comp.CurrentLength = args.GetValueAsFloat();
            foreach (var rope in EnumerateRopes(ent))
                _ropes.SetRopeLength(rope, ent.Comp.CurrentLength);
        }
        else if (args.Group.Id == "preset")
        {
            ent.Comp.RopeConfig = args.Value;
            RefreshRopes(ent, true);
        }
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
