using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._Floof.Popup;

public sealed class PopUpOnUseSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PopUpOnUseComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PopUpOnUseComponent, PopUpOnUseDoAfterEvent>(OnDoAfter);
    }

    // Whitelist what we can and can't use these items on (no using this stuff on a wall smh)
    // Thank you, Fox, for the help getting the whitelist working
    public bool CanUse(Entity<PopUpOnUseComponent> ent, EntityUid target) =>
        _whitelist.IsWhitelistPass(ent.Comp.Whitelist, target) &&
        !_whitelist.IsWhitelistPass(ent.Comp.Blacklist, target);

    private void OnAfterInteract(Entity<PopUpOnUseComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !args.CanReach)
            return;

        // Check if the entitiy the item will be used on is blacklisted, if it is return
        if (!CanUse(ent, args.Target.Value))
            return;

        if (_net.IsServer)
        {
            // Cannot cancel predicted audio.
            _audio.Stop(ent.Comp.AudioStream);
            ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.PopUpDoAfterSound, ent)?.Entity;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.PopUpDoAfterTime, new PopUpOnUseDoAfterEvent(), ent, args.Target.Value, ent)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnDoAfter(Entity<PopUpOnUseComponent> ent, ref PopUpOnUseDoAfterEvent args)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);

        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        // For some reason, no one but the user can predict the do-after. Oh well.
        var locKey = args.Target == args.User ? ent.Comp.UserPopUpMsg : ent.Comp.TargetPopUpMsg;
        _popup.PopupPredicted(
            Loc.GetString(locKey,
                ("target", Identity.Entity(args.Target.Value, EntityManager)),
                ("user", Identity.Entity(args.User, EntityManager)),
                ("used", Identity.Entity(args.Used ?? EntityUid.Invalid, EntityManager))),
            args.User,
            args.User);

        if (ent.Comp.Repeat)
        {
            args.Repeat = true;
            // On the server side, repeat the sound
            if (_net.IsServer)
                ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.PopUpDoAfterSound, ent)?.Entity;
        }

        args.Handled = true;
    }
}
