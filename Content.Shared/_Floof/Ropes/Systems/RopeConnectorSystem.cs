using Content.Shared._Euphoria.Selector.Events;
using Content.Shared._Floof.Paint;
using Content.Shared._Floof.Ropes.Components;
using Content.Shared._Floof.Ropes.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._Floof.Ropes.Systems;

/// <summary>
///     Notes on terminology:
///     - Rope = a rope entity managed by the rope system
///     - Rope connector = an item that a player can use to connect ropes to objects
///     - Anchor = an object to which a rope is connected
///
///     Rope sides:
///     - Start - initially attached to a rope connector
///     - End - initially free, attached to a handle or another object
///     - Master - side that is attached to the connector. Once it is attached to something, the connector gets removed from the user's hands.
/// </summary>
public sealed class RopeConnectorSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RopeSystem _ropes = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelists = default!;
    [Dependency] private readonly SharedColorPaintSystem _paint = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfters = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RopeConnectorAttachedComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAttachedVerbs);
        SubscribeLocalEvent<RopeConnectorComponent, GetVerbsEvent<UtilityVerb>>(OnGetConnectorVerbs);
        SubscribeLocalEvent<RopeConnectorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RopeConnectorComponent, ColorPaintChangedEvent>(OnColorPainted);
        SubscribeLocalEvent<RopeConnectorComponent, ConfigurationSelectedEvent>(OnConfigured);

        SubscribeLocalEvent<RopeConnectorComponent, RopeConnectorAttachedDoAfterEvent>(OnAttachedDoAfter);
        SubscribeLocalEvent<RopeConnectorAttachedComponent, RopeConnectorDetachedDoAfterEvent>(OnDetachedDoAfter);

        SubscribeLocalEvent<RopeConnectorRopeComponent, ComponentShutdown>(OnRopeShutdown);
    }

    private void OnGetConnectorVerbs(Entity<RopeConnectorComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        foreach (var side in Enum.GetValues<RopeSide>())
            TryAddAnchorVerb(ent, args.User, args.Target, args, side);

        void TryAddAnchorVerb(Entity<RopeConnectorComponent> connector, EntityUid user, EntityUid target, GetVerbsEvent<UtilityVerb> args, RopeSide side)
        {
            var canAttach = CanAttach(connector, target, side, out var reasonLoc);
            var verb = new UtilityVerb()
            {
                DoContactInteraction = true,
                CloseMenu = true,
                Text = Loc.GetString($"rope-connector-attach-{side}"),
                Message = reasonLoc != null ? Loc.GetString(reasonLoc) : null,
                IconEntity = GetNetEntity(connector),
                Disabled = !canAttach,
                Act = () =>
                {
                    if (canAttach)
                        StartAttaching(connector, target, user, side);
                },
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnGetAttachedVerbs(Entity<RopeConnectorAttachedComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var user = args.User;
        var connector = ent.Comp.Connector;
        var (canDetach, side) = CanDetach(ent, args.User);
        var verb = new AlternativeVerb()
        {
            DoContactInteraction = true,
            CloseMenu = true,
            Text = Loc.GetString($"rope-connector-detach"),
            // Message = reason,
            IconEntity = GetNetEntity(connector),
            Disabled = !canDetach,
            Act = () =>
            {
                if (canDetach)
                    StartDetaching(ent, user);
            },
        };
        args.Verbs.Add(verb);
    }

    private void OnShutdown(Entity<RopeConnectorComponent> ent, ref ComponentShutdown args)
    {
        // Clean up attached entities on shutdown
        if (ent.Comp.RopeEntity is { Valid: true } rope)
            PredictedQueueDel(rope);

        if (ent.Comp.HandleEntity is { Valid: true } handle)
            PredictedQueueDel(handle);
    }

    private void OnColorPainted(Entity<RopeConnectorComponent> ent, ref ColorPaintChangedEvent args)
    {
        if (GetRope(ent) is { } rope)
            _ropes.SetRopeColor(rope.AsNullable(), args.NewColor);
    }

    private void OnConfigured(Entity<RopeConnectorComponent> ent, ref ConfigurationSelectedEvent args)
    {
        if (args.Group.Id == "length")
        {
            ent.Comp.CurrentLength = args.GetValueAsFloat();
            if (ent.Comp.RopeEntity is {} rope)
                _ropes.SetRopeLength(rope, ent.Comp.CurrentLength);
        }
    }

    private void OnAttachedDoAfter(Entity<RopeConnectorComponent> connector, ref RopeConnectorAttachedDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // If there's a handle, delete it first
        if (connector.Comp.HandleEntity is {} handle)
            QueueDel(handle);

        var side = args.Side;
        var anchor = args.Target!.Value;

        TryAttach(connector, anchor, side);
    }

    private void OnDetachedDoAfter(Entity<RopeConnectorAttachedComponent> anchor, ref RopeConnectorDetachedDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        TryDetach(anchor, args.User);
    }

    private void OnRopeShutdown(Entity<RopeConnectorRopeComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<RopeConnectorComponent>(ent.Comp.Connector, out var connectorComp)
            || !TryComp<RopeComponent>(ent, out var ropeComp))
            return;

        // Cleanup anchors
        foreach (var anchor in _ropes.EnumerateAnchors((ent.Owner, ropeComp)))
            RemCompDeferred<RopeAttachedComponent>(anchor);

        if (connectorComp.RopeEntity == ent)
            connectorComp.RopeEntity = null;
    }

    private void StartAttaching(Entity<RopeConnectorComponent> connector, EntityUid target, EntityUid user, RopeSide side)
    {
        var args = new DoAfterArgs(EntityManager,
            user,
            connector.Comp.ConnectDelay,
            new RopeConnectorAttachedDoAfterEvent(connector, side),
            connector,
            target,
            connector)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfters.TryStartDoAfter(args);
    }

    public void StartDetaching(Entity<RopeConnectorAttachedComponent> anchor, EntityUid user)
    {
        var args = new DoAfterArgs(EntityManager,
            user,
            anchor.Comp.DetachDelay,
            new RopeConnectorDetachedDoAfterEvent(),
            anchor,
            anchor,
            null)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = false,
            BreakOnDropItem = false, // remove this if removing rope connectors ever starts requiring tools
        };

        _doAfters.TryStartDoAfter(args);
    }

    public bool TryAttach(Entity<RopeConnectorComponent> connector, EntityUid anchor, RopeSide side)
    {
        if (!CanAttach(connector, anchor, side, out var reason))
            return false;

        if (GetOrCreateRope(connector) is not {} rope)
            return false;

        var isMaster = side == connector.Comp.MasterSide;
        // If this is the master side, we must remove the rope out of the user's hands first and detach the rope start
        if (isMaster)
        {
            if (!_containers.TryRemoveFromContainer(connector.Owner, false, out var inContainer) && inContainer)
                return false;
        }

        _ropes.TryDetachRopeSide(rope.AsNullable(), side);
        var result = _ropes.TryConnectRopeSide(rope.AsNullable(), anchor, side);
        if (!result)
            return false;

        _ropes.DistributeLinksBetweenAnchors(rope.AsNullable(), true);

        var attachedComp = EnsureComp<RopeConnectorAttachedComponent>(anchor);
        attachedComp.Connector = connector;
        attachedComp.Side = side;
        attachedComp.DetachDelay = connector.Comp.ConnectDelay;

        // If this is the master, we take the rope out of the user's hands and put it in a container on this connector
        if (isMaster)
        {
            var container = _containers.EnsureContainer<Container>(anchor, RopeConnectorAttachedComponent.ConnectorContainer);
            _containers.Insert(connector.Owner, container);
        }

        Dirty(connector);
        Dirty(anchor, attachedComp);
        return true;
    }

    private bool TryDetach(Entity<RopeConnectorAttachedComponent> anchor, EntityUid user)
    {
        if (CanDetach(anchor, user) is not (true, var side))
            return false;

        var connector = anchor.Comp.Connector;
        if (!TryComp<RopeConnectorComponent>(connector, out var connectorComp) || GetRope((connector, connectorComp)) is not { } rope)
            return false;

        // I tried, i genuinely tried. But the amount of edge cases... It was INSANE.
        // Just delete the rope and forget it ever existed. This is better for everyone's sanity.
        PredictedQueueDel(connectorComp.RopeEntity);
        connectorComp.RopeEntity = null;

        // If the connector is currently inside a container, it means that both of its rope sides have been attached
        // We need to extract it from the container and set whichever side that was removed as the new master
        if (_containers.TryGetContainingContainer(connector, out var connectorContainer)
            && connectorContainer.ID == RopeConnectorAttachedComponent.ConnectorContainer)
        {
            if (!_containers.TryRemoveFromContainer(connector))
                return false;

            _hands.PickupOrDrop(user, connector, true, true, true, true);
            connectorComp.MasterSide = side;
        }

        // Just so it doesn't allow a second detach on the same tick
        anchor.Comp.Connector = EntityUid.Invalid;
        anchor.Comp.CanDetach = false;
        RemCompDeferred(anchor, anchor.Comp);

        Dirty(connector, connectorComp);
        return true;
    }

    public bool CanAttach(Entity<RopeConnectorComponent> connector, EntityUid anchor, RopeSide side, out string? reasonLoc)
    {
        if (_whitelists.IsWhitelistFail(connector.Comp.TargetWhitelist, anchor)
            || _whitelists.IsWhitelistPass(connector.Comp.TargetBlacklist, anchor))
        {
            reasonLoc = "rope-connector-whitelist-fail";
            return false;
        }

        // Is the relevant side already attached?
        var currentAnchorData = GetAnchorInfo(connector, side);
        if (currentAnchorData?.Anchor is { Valid: true } curAnchor && curAnchor != connector.Owner)
        {
            reasonLoc = "rope-connector-already-attached";
            return false;
        }

        // By default, the start side is connected to the connector
        // We allow to connect the start side only after the end side
        if (side == connector.Comp.MasterSide && GetAnchorInfo(connector, connector.Comp.MasterSide.Opposite()) == null)
        {
            reasonLoc = "rope-connector-attach-end-first";
            return false;
        }

        if (!connector.Comp.ConnectableSides.Contains(side))
        {
            reasonLoc = "rope-connector-cant-attach-side";
            return false;
        }

        // I couldn't be bothered to allow connecting multiple ropes to the same anchor here
        if (TryComp<RopeConnectorComponent>(anchor, out var existingAnchor) && existingAnchor.RopeEntity is { Valid: true })
        {
            reasonLoc = "rope-connector-anchor-already-attached";
            return false;
        }

        reasonLoc = null;
        return true;
    }

    public (bool, RopeSide) CanDetach(Entity<RopeConnectorAttachedComponent> anchor, EntityUid user)
    {
        if (anchor.Comp.Connector is not { Valid: true } connector)
            return (false, default);

        if (!anchor.Comp.CanDetach || HasComp<UnremoveableComponent>(connector))
            return (false, default);

        // the rest?
        return (true, anchor.Comp.Side);
    }

    private Entity<RopeComponent>? GetRope(Entity<RopeConnectorComponent> connector)
    {
        if (connector.Comp.RopeEntity is { Valid: true } rope && TryComp<RopeComponent>(rope, out var ropeComp))
            return (rope, ropeComp);

        return null;
    }

    private Entity<RopeComponent>? GetOrCreateRope(Entity<RopeConnectorComponent> connector)
    {
        if (GetRope(connector) is { } existing)
            return existing;

        if (_net.IsClient)
            return null;

        // Just in case
        if (connector.Comp.RopeEntity is { } invalidRope)
            TryQueueDel(invalidRope);

        connector.Comp.MasterSide = RopeSide.Start; // Nothing we can do
        if (!_ropes.TryCreateRope(connector, null, connector.Comp.RopePrototype, connector.Comp.CurrentLength, out var rope))
            return null;

        EnsureComp<RopeConnectorRopeComponent>(rope.Value).Connector = connector;

        connector.Comp.RopeEntity = rope;
        Dirty(connector);

        _ropes.SetRopeColor(rope.Value!, _paint.GetEffectiveColor(connector));
        return rope;
    }

    private RopeComponent.AnchorInfo? GetAnchorInfo(Entity<RopeConnectorComponent> connector, RopeSide side) =>
        GetRope(connector) is { } rope ? _ropes.GetAnchor(rope, side) : null;
}
