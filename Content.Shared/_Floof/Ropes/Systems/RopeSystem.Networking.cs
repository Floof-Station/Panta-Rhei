using Content.Shared._Floof.Ropes.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    private void InitializeNetworking()
    {
        SubscribeLocalEvent<RopeComponent, ComponentGetState>(OnRopeGetState);
        SubscribeLocalEvent<RopeComponent, ComponentHandleState>(OnRopeHandleState);
    }

    private void OnRopeGetState(Entity<RopeComponent> ent, ref ComponentGetState args)
    {
        args.State = new RopeComponent.State(ent.Comp, EntityManager);
    }

    private void OnRopeHandleState(Entity<RopeComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not RopeComponent.State { } state)
            return;

        state.Apply(ent.Comp, EntityManager);

        // Go through each link and add RopeLinkComponent, which is non-netsynced
        foreach (var link in ent.Comp.Links)
        {
            if (!link.LinkEntity.Valid)
                continue;

            var linkComp = EnsureComp<RopeLinkComponent>(link.LinkEntity);
            linkComp.Rope = ent;
        }
    }
}
