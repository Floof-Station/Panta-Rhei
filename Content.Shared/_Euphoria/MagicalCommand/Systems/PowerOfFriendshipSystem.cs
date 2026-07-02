using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Euphoria.MagicalCommand;

public sealed class PowerOfFriendshipSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PowerOfFriendshipComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<PowerOfFriendshipComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<FriendshipContributorComponent>(args.Used, out var friendship) || args.Handled)
            return;

        args.Handled = AddContributor(ent, (args.Used, friendship), args.User);
    }

    private bool AddContributor(Entity<PowerOfFriendshipComponent> target,
        Entity<FriendshipContributorComponent> contributor,
        EntityUid user)
    {
        if (target.Comp.Contributors.Contains(contributor) || target.Comp.Keyword != contributor.Comp.Keyword)
            return false;

        if (_inventory.InSlotWithFlags(target.Owner, SlotFlags.OUTERCLOTHING))
        {
            _popups.PopupClient(Loc.GetString("friendship-failure-equipped", ("target", target)), target, user);
            return false;
        }

        target.Comp.Contributors.Add(contributor);

        if (target.Comp.Contributors.Count >= target.Comp.ContributorsRequired)
        {
            var super = PredictedSpawnNextToOrDrop(target.Comp.EmpoweredResult, target.Owner);
            _popups.PopupPredicted(Loc.GetString("friendship-empowered-transformation", ("target", target)), super, user);
            var ev = new FriendshipTransformEvent
            {
                InitialEnt = target,
                CreatedEnt = super,
            };
            RaiseLocalEvent(target, ev);
            PredictedQueueDel(target.Owner);
        }
        else
            _popups.PopupClient(Loc.GetString("friendship-contribute-success", ("target", target), ("used", contributor)), target, user);

        Dirty(target);

        return true;
    }
}

public sealed partial class FriendshipTransformEvent : EntityEventArgs
{
    public EntityUid InitialEnt;
    public EntityUid CreatedEnt;
}
