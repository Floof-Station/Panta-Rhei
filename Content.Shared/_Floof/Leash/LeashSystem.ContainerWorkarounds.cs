using Content.Shared._Floof.Leash.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    private void InitializeContainerWorkarounds()
    {
        SubscribeLocalEvent<LeashedComponent, EntGotInsertedIntoContainerMessage>(OnLeashedContainerChanged);
        SubscribeLocalEvent<LeashedComponent, EntGotRemovedFromContainerMessage>(OnLeashedContainerChanged);

        SubscribeLocalEvent<LeashComponent, EntGotInsertedIntoContainerMessage>(OnLeashContainerChanged);
        SubscribeLocalEvent<LeashComponent, EntGotRemovedFromContainerMessage>(OnLeashContainerChanged);
    }

    // Note: we can't use the Entity<T> handler here because it doesn't support polymorphism
    private void OnLeashedContainerChanged(EntityUid ent, LeashedComponent comp, ContainerModifiedMessage args)
    {
        // TODO: this method refreshes all leashes instead of just the one that changed.
        // This doesn't matter much because most leashes can only create 1 joint, but look into fixing this.
        if (!_net.IsClient && GetEntity(comp.Leash) is { } leashEnt && TryComp<LeashComponent>(leashEnt, out var leash))
            RefreshJoints((leashEnt, leash));
    }

    private void OnLeashContainerChanged(EntityUid ent, LeashComponent comp, ContainerModifiedMessage args)
    {
        if (!_net.IsClient)
            RefreshJoints((ent, comp));
    }
}
