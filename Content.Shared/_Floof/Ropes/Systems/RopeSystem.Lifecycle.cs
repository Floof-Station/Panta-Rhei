using Content.Shared._Floof.Ropes.Components;
using Content.Shared.Popups;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    // All ropes that may need to be re-created on the next tick
    private HashSet<EntityUid> _pendingRopeUpdates = new(10);

    public void InitializeLifecycle()
    {
        SubscribeLocalEvent<RopeComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<RopeLinkComponent, ComponentShutdown>(OnLinkShutdown);
        SubscribeLocalEvent<RopeLinkComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<RopeLinkComponent, JointBreakEvent>(OnJointBroken);
    }

    public override void Update(float frameTime)
    {
        foreach (var rope in _pendingRopeUpdates)
        {
            if (TerminatingOrDeleted(rope) || !_ropeQuery.TryComp(rope, out var ropeComp))
                continue;

            if (!EnableRope(rope))
            {
                Log.Warning($"Rope {ToPrettyString(rope)} cannot be re-enabled. Deleting it.");
                QueueDel(rope);
            }
        }
        _pendingRopeUpdates.Clear();
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

    private void OnLinkShutdown(Entity<RopeLinkComponent> link, ref ComponentShutdown args)
    {
        if (_net.IsClient || TerminatingOrDeleted(link.Comp.Rope))
            return;

        if (TryQueueDel(link.Comp.Rope))
            Log.Info($"One of the links of {link.Comp.Rope} is deleted. Deleting the rope");
    }

    private void OnJointRemoved(Entity<RopeLinkComponent> link, ref JointRemovedEvent args)
    {
        if (_net.IsClient
            || IsDisabled(link.Comp.Rope) // most likely
            || TerminatingOrDeleted(link.Comp.Rope)
            || TerminatingOrDeleted(link))
            return;

        Log.Info($"Joint {args.Joint.ID} is removed. Will try to recreate the rope on the next tick.");
        _pendingRopeUpdates.Add(link.Comp.Rope);

        DisableRope(link.Comp.Rope);
    }

    private void OnJointBroken(Entity<RopeLinkComponent> link, ref JointBreakEvent args)
    {
        if (_net.IsClient || TerminatingOrDeleted(link.Comp.Rope))
            return;

        if (!TryQueueDel(link.Comp.Rope))
            return;

        if (_xform.TryGetMapOrGridCoordinates(link, out var coords))
            _popups.PopupCoordinates(Loc.GetString("rope-destroyed-popup", ("rope", link.Comp.Rope)), coords.Value, PopupType.Medium);
    }
}
