using Content.Shared.Doors;
using Content.Shared.Emag.Components;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Robust.Shared.Network;


namespace Content.Shared._Floof.Lock;


/// <summary>
///     Prevents locked doors from being opened.
/// </summary>
public sealed class DoorLockSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LockComponent, BeforeDoorOpenedEvent>(OnDoorOpenAttempt);
    }

    private void OnDoorOpenAttempt(Entity<LockComponent> ent, ref BeforeDoorOpenedEvent args)
    //Will need to test this, since I believe this was supposed to break the lock on emag so that is what I attempted here
    {
        if (!ent.Comp.Locked || ent.Comp.BreakOnAccessBreaker && HasComp<EmaggedComponent>(ent))
            return;

        args.Cancel();
        if (args.User is {} user && _net.IsServer)
            _popup.PopupCursor(Loc.GetString("entity-storage-component-locked-message"), user);
    }
}
