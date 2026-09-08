using Content.Shared._Floof.Ropes.Components;
using Content.Shared.Popups;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public void InitializeLifecycle()
    {
        SubscribeLocalEvent<RopeComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<RopeLinkComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<RopeLinkComponent, JointBreakEvent>(OnJointBroken);
    }

    private void OnShutdown(Entity<RopeComponent> ent, ref ComponentShutdown args)
    {
        // On shutdown, destroy all links
        foreach (var link in ent.Comp.Links)
        {
            // Client can have these set to EntityUid.Invalid during network sync
            if (link.LinkEntity.Valid)
                PredictedQueueDel(link.LinkEntity);
        }
    }

    private void OnJointRemoved(Entity<RopeLinkComponent> link, ref JointRemovedEvent args)
    {
        if (_net.IsClient || TerminatingOrDeleted(link.Comp.Rope))
            return;

        Log.Warning($"Joint {args.Joint.ID} is removed. Deleting the rope.");
        TryQueueDel(link.Comp.Rope);
    }

    private void OnJointBroken(Entity<RopeLinkComponent> link, ref JointBreakEvent args)
    {
        if (_net.IsClient || TerminatingOrDeleted(link.Comp.Rope))
            return;

        if (!TryQueueDel(link.Comp.Rope))
            return;

        _popups.PopupEntity(Loc.GetString("rope-destroyed-popup", ("rope", link.Comp.Rope)), link.Comp.Rope);
    }
}
