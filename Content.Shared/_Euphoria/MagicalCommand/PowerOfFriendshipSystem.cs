using Content.Shared.Interaction;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Euphoria.MagicalCommand;

public sealed class PowerOfFriendshipSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PowerOfFriendshipComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<PowerOfFriendshipComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<FriendshipContributorComponent>(args.Used, out var friendship) || args.Handled)
            return;

        args.Handled = AddContributor(ent, (args.Used, friendship));
    }

    private bool AddContributor(Entity<PowerOfFriendshipComponent> target,
        Entity<FriendshipContributorComponent> contributor)
    {
        if (target.Comp.Contributors.Contains(contributor)
            || target.Comp.Contributors.Count >= target.Comp.ContributorsRequired)
            return false;

        // Todo add feedback of some kind to show that it worked
        // a popup? Maybe say how many you need left? Could also put that in the description
        target.Comp.Contributors.Add(contributor);

        if (target.Comp.Contributors.Count >= target.Comp.ContributorsRequired)
        {
            // a popup here could be cool too
            PredictedSpawnNextToOrDrop(target.Comp.EmpoweredResult, target.Owner);
            PredictedQueueDel(target.Owner);
        }

        return true;
    }
}
