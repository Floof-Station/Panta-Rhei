using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Organ;

namespace Content.Shared._Floof.Body.Systems;

// This used to be edited into the BodySystem pre-rebase
public sealed class OrganRottingSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<OrganComponent, IsRottingEvent>(OnCheckRotting);
    }

    private void OnCheckRotting(Entity<OrganComponent> ent, ref IsRottingEvent args)
    {
        // Check if the body exists. If so, do not allow rotting to progress.
        // This won't reset rotting, so med has to be careful when transplanting organs.
        args.Handled |= Exists(ent.Comp.Body);
    }
}
