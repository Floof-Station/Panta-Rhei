using Content.Shared._Floof.Ropes.Components;
using Content.Shared._Floof.Util;
using Content.Shared.Popups;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem
{
    // All ropes that may need to be re-created on the next tick
    private HashSet<EntityUid> _pendingRopeUpdates = new(10);

    private Ticker ForcedRopeUpdateTicker = new(TimeSpan.FromSeconds(5));

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

            UpdateRope((rope, ropeComp));
        }
        _pendingRopeUpdates.Clear();

        // Every few seconds we update every rope in case something like carrying has occurred
        if (ForcedRopeUpdateTicker.TryUpdate(_timing))
        {
            var query = EntityQueryEnumerator<RopeComponent>();
            while (query.MoveNext(out var uid, out var rope))
            {
                UpdateRope((uid, rope));
            }
            query.Dispose();
        }
    }

    private void OnShutdown(Entity<RopeComponent> ent, ref ComponentShutdown args)
    {
        // So other things don't attempt to queue the rope for re-creation
        ent.Comp.IsDisabled = true;

        // On shutdown, destroy all links
        foreach (var link in ent.Comp.Links)
        {
            // Client can have these set to EntityUid.Invalid during network sync
            if (link.LinkEntity.Valid)
                PredictedQueueDel(link.LinkEntity);
        }

        // In case its a linkless rope, also destroy the start anchor joint (which is the same as the last)
        if (ent.Comp.ConnectedStart is {} start)
        {
            _joints.RemoveJoint(start.Anchor, start.JointId);
            OnRopeDetached(ent!, start.Anchor);
        }
        if (ent.Comp.ConnectedEnd is {} end)
        {
            _joints.RemoveJoint(end.Anchor, end.JointId);
            OnRopeDetached(ent!, end.Anchor);
        }
    }

    private void OnLinkShutdown(Entity<RopeLinkComponent> link, ref ComponentShutdown args)
    {
        if (_net.IsClient || !link.Comp.Rope.IsValid() || TerminatingOrDeleted(link.Comp.Rope))
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

        // TODO: this is a suboptimal way. In most cases, we only need to recreate one of the joints connecting the rope to one of the anchors
        Log.Info($"Joint {args.Joint.ID} is removed. Will try to recreate the rope on the next tick.");
        RecreateRope(link.Comp.Rope);
    }

    /// <summary>
    ///     Disables and tries to respawn the rope on the neck tick.
    /// </summary>
    public void RecreateRope(EntityUid rope)
    {
        _pendingRopeUpdates.Add(rope);
        DisableRope(rope);
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
