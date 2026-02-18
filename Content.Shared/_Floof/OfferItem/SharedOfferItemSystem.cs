using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Robust.Shared.Network;
using Robust.Shared.Timing;

// Dear contributor.
// This system is fucking unmaintainable.
// If you ever happen to touch this again, please do your best to document your changes and try to resolve mysteries surrounding this code.
// I did what I could to document the parts I manage to understand, but there is still more truth to be unveiled.
//
// HOURS_WASTED_HERE_FLOOFSTATION = 8

namespace Content.Shared._Floof.OfferItem;

public abstract partial class SharedOfferItemSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OfferItemComponent, InteractUsingEvent>(SetInReceiveMode);
        SubscribeLocalEvent<OfferItemComponent, MoveEvent>(OnMove);

        InitializeInteractions();
    }

    private void SetInReceiveMode(EntityUid receiver, OfferItemComponent receiverComponent, InteractUsingEvent args)
    {
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState)
            return;

        if (!TryComp<OfferItemComponent>(args.User, out var offererComponent))
            return;

        var offerer = args.User;
        if (offerer == receiver || receiverComponent.IsInReceiveMode || !offererComponent.IsInOfferMode ||
            (offererComponent.IsInReceiveMode && offererComponent.TargetOrOfferer != receiver))
            return;

        receiverComponent.IsInReceiveMode = true;
        receiverComponent.TargetOrOfferer = args.User;

        Dirty(receiver, receiverComponent);

        offererComponent.TargetOrOfferer = receiver;
        offererComponent.IsInOfferMode = false; // FLOOFSTATION - WHAT????? WHY????

        Dirty(args.User, offererComponent);

        if (offererComponent.Item == null)
            return;

        // Sender popup (client-side only)
        _popup.PopupClient(
            Loc.GetString("offer-item-try-give",
                ("item", Identity.Entity(offererComponent.GetRealEntity(EntityManager), EntityManager)),
                ("target", Identity.Entity(receiver, EntityManager))),
            offerer,
            offerer);
        // Receiver popup (server side only, not predicted because recipient != local player)
        _popup.PopupEntity(
            Loc.GetString("offer-item-try-give-target",
                ("user", Identity.Entity(receiverComponent.TargetOrOfferer.Value, EntityManager)),
                ("item", Identity.Entity(offererComponent.GetRealEntity(EntityManager), EntityManager))),
            offerer,
            receiver);

        args.Handled = true;
    }

    private void OnMove(EntityUid uid, OfferItemComponent component, MoveEvent args)
    {
        if (_net.IsClient) // Client often mispredicts movement, we cant trust it here
            return;

        if (component.TargetOrOfferer == null ||
            args.NewPosition.InRange(EntityManager, _transform,
                Transform(component.TargetOrOfferer.Value).Coordinates, component.MaxOfferDistance))
            return;

        UnOffer(uid, component);
    }

    /// <summary>
    /// Resets the <see cref="_Floof.OfferItem.OfferItemComponent"/> of the user and the target
    /// </summary>
    protected void UnOffer(EntityUid thisEntity, OfferItemComponent offererComp)
    {
        if (!TryComp<HandsComponent>(thisEntity, out var hands) || _hands.GetActiveHand((thisEntity, hands)) is null)
            return;

        if (offererComp.TargetOrOfferer is {} otherEntity && TryComp<OfferItemComponent>(otherEntity, out var otherOfferer))
        {
            // So this tries to figure out which of these entities do what...
            // if A.OfferItemComponent.Item != null, then A is currently offering an item to A.OfferItemComponent.TargetOrOfferer
            // If it is null, then it is ONLY being offered an item TO.
            if (offererComp.Item != null && _net.IsServer)
            {
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give",
                        ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager)), // Floof - resolve virtual items
                        ("target", Identity.Entity(otherEntity, EntityManager))),
                    thisEntity,
                    thisEntity);
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give-target",
                        ("user", Identity.Entity(thisEntity, EntityManager)),
                        ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager))),
                    thisEntity,
                    otherEntity);
            }

            else if (otherOfferer.Item != null && _net.IsServer)
            {
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give",
                        ("item", Identity.Entity(otherOfferer.GetRealEntity(EntityManager), EntityManager)), // Floof - resolve virtual items
                        ("target", Identity.Entity(thisEntity, EntityManager))),
                    otherEntity,
                    otherEntity);
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give-target",
                        ("user", Identity.Entity(otherEntity, EntityManager)),
                        ("item", Identity.Entity(otherOfferer.GetRealEntity(EntityManager), EntityManager))),
                    otherEntity,
                    thisEntity);
            }

            otherOfferer.IsInOfferMode = false;
            otherOfferer.IsInReceiveMode = false;
            otherOfferer.Hand = null;
            otherOfferer.TargetOrOfferer = null;
            otherOfferer.Item = null;

            Dirty(otherEntity, otherOfferer);
        }

        offererComp.IsInOfferMode = false;
        offererComp.IsInReceiveMode = false;
        offererComp.Hand = null;
        offererComp.TargetOrOfferer = null;
        offererComp.Item = null;

        Dirty(thisEntity, offererComp);
    }


    /// <summary>
    /// Cancels the transfer of the item
    /// </summary>
    protected void UnReceive(EntityUid receiver, OfferItemComponent? receiverComp = null, OfferItemComponent? offererComp = null)
    {
        if (!Resolve(receiver, ref receiverComp)
            || receiverComp.TargetOrOfferer is not {} offerer
            || !Resolve(offerer, ref offererComp))
            return;

        // Idk why this check is here
        if (!TryComp<HandsComponent>(receiver, out var hands) || _hands.GetActiveHand((receiver, hands)) == null || receiverComp.TargetOrOfferer == null)
            return;

        // If offererComp.Item != null, then they are actively offering to TargetOrOfferer
        // Normally this method is called right after a transfer is done and item is set to false, so this is never called ig?
        if (offererComp.Item != null)
        {
            _popup.PopupEntity(
                Loc.GetString("offer-item-no-give",
                    ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager)), // Floof - resolve virtual items
                    ("target", Identity.Entity(receiver, EntityManager))),
                offerer,
                offerer);
            _popup.PopupEntity(
                Loc.GetString("offer-item-no-give-target",
                    ("user", Identity.Entity(receiverComp.TargetOrOfferer.Value, EntityManager)), // Floof - resolve virtual items
                    ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager))),
                offerer,
                receiver);
        }

        if (!offererComp.IsInReceiveMode)
        {
            offererComp.TargetOrOfferer = null;
            receiverComp.TargetOrOfferer = null;
        }

        offererComp.Item = null;
        offererComp.Hand = null;
        receiverComp.IsInReceiveMode = false;

        Dirty(receiver, receiverComp);
    }

    /// <summary>
    /// Returns true if <see cref="_Floof.OfferItem.OfferItemComponent.IsInOfferMode"/> = true
    /// </summary>
    protected bool IsInOfferMode(EntityUid? entity, OfferItemComponent? component = null)
    {
        return entity != null && Resolve(entity.Value, ref component, false) && component.IsInOfferMode;
    }
}
