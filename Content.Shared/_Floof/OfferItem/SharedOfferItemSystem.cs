using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Robust.Shared.Network;
using Robust.Shared.Timing;

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

    private void SetInReceiveMode(EntityUid uid, OfferItemComponent component, InteractUsingEvent args)
    {
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState)
            return;

        if (!TryComp<OfferItemComponent>(args.User, out var offerItem))
            return;

        if (args.User == uid || component.IsInReceiveMode || !offerItem.IsInOfferMode ||
            (offerItem.IsInReceiveMode && offerItem.Target != uid))
            return;

        component.IsInReceiveMode = true;
        component.Target = args.User;

        Dirty(uid, component);

        offerItem.Target = uid;
        offerItem.IsInOfferMode = false;

        Dirty(args.User, offerItem);

        if (offerItem.Item == null)
            return;

        // Floof - if the held item is a pseudo-item, show the underlying item in the popup
        _popup.PopupPredicted(Loc.GetString("offer-item-try-give",
            ("item", Identity.Entity(offerItem.GetRealEntity(EntityManager), EntityManager)), // FLoof - resolve virtual items
            ("target", Identity.Entity(uid, EntityManager))), component.Target.Value, component.Target.Value);
        _popup.PopupPredicted(Loc.GetString("offer-item-try-give-target",
            ("user", Identity.Entity(component.Target.Value, EntityManager)),
            ("item", Identity.Entity(offerItem.GetRealEntity(EntityManager), EntityManager))), component.Target.Value, uid); // FLoof - resolve virtual items

        args.Handled = true;
    }

    private void OnMove(EntityUid uid, OfferItemComponent component, MoveEvent args)
    {
        if (_net.IsClient) // Client often mispredicts movement, we cant trust it here
            return;

        if (component.Target == null ||
            args.NewPosition.InRange(EntityManager, _transform,
                Transform(component.Target.Value).Coordinates, component.MaxOfferDistance))
            return;

        UnOffer(uid, component);
    }

    /// <summary>
    /// Resets the <see cref="_Floof.OfferItem.OfferItemComponent"/> of the user and the target
    /// </summary>
    protected void UnOffer(EntityUid uid, OfferItemComponent component)
    {
        if (!TryComp<HandsComponent>(uid, out var hands) || _hands.GetActiveHand((uid, hands)) is null)
            return;

        if (TryComp<OfferItemComponent>(component.Target, out var offerItem) && component.Target != null)
        {

            if (component.Item != null)
            {
                _popup.PopupPredicted(Loc.GetString("offer-item-no-give",
                    ("item", Identity.Entity(component.GetRealEntity(EntityManager), EntityManager)), // FLoof - resolve virtual items
                    ("target", Identity.Entity(component.Target.Value, EntityManager))), uid, uid);
                _popup.PopupPredicted(Loc.GetString("offer-item-no-give-target",
                    ("user", Identity.Entity(uid, EntityManager)),
                    ("item", Identity.Entity(component.GetRealEntity(EntityManager), EntityManager))), uid, component.Target.Value); // FLoof - resolve virtual items
            }

            else if (offerItem.Item != null)
            {
                _popup.PopupPredicted(Loc.GetString("offer-item-no-give",
                    ("item", Identity.Entity(offerItem.GetRealEntity(EntityManager), EntityManager)), // FLoof - resolve virtual items
                    ("target", Identity.Entity(uid, EntityManager))), component.Target.Value, component.Target.Value);
                _popup.PopupPredicted(Loc.GetString("offer-item-no-give-target",
                    ("user", Identity.Entity(component.Target.Value, EntityManager)),
                    ("item", Identity.Entity(offerItem.GetRealEntity(EntityManager), EntityManager))), component.Target.Value, uid); // FLoof - resolve virtual items
            }

            offerItem.IsInOfferMode = false;
            offerItem.IsInReceiveMode = false;
            offerItem.Hand = null;
            offerItem.Target = null;
            offerItem.Item = null;

            Dirty(component.Target.Value, offerItem);
        }

        component.IsInOfferMode = false;
        component.IsInReceiveMode = false;
        component.Hand = null;
        component.Target = null;
        component.Item = null;

        Dirty(uid, component);
    }


    /// <summary>
    /// Cancels the transfer of the item
    /// </summary>
    protected void UnReceive(EntityUid uid, OfferItemComponent? component = null, OfferItemComponent? offerItem = null)
    {
        if (component == null && !TryComp(uid, out component))
            return;

        if (offerItem == null && !TryComp(component.Target, out offerItem))
            return;

        if (!TryComp<HandsComponent>(uid, out var hands) || _hands.GetActiveHand((uid, hands)) == null || component.Target == null)
            return;

        if (offerItem.Item != null)
        {
            _popup.PopupEntity(Loc.GetString("offer-item-no-give",
                ("item", Identity.Entity(offerItem.GetRealEntity(EntityManager), EntityManager)), // Floof - resolve virtual items
                ("target", Identity.Entity(uid, EntityManager))),
                component.Target.Value,
                component.Target.Value);
            _popup.PopupEntity(Loc.GetString("offer-item-no-give-target",
                ("user", Identity.Entity(component.Target.Value, EntityManager)), // Floof - resolve virtual items
                ("item", Identity.Entity(offerItem.GetRealEntity(EntityManager), EntityManager))),
                component.Target.Value,
                uid);
        }

        if (!offerItem.IsInReceiveMode)
        {
            offerItem.Target = null;
            component.Target = null;
        }

        offerItem.Item = null;
        offerItem.Hand = null;
        component.IsInReceiveMode = false;

        Dirty(uid, component);
    }

    /// <summary>
    /// Returns true if <see cref="_Floof.OfferItem.OfferItemComponent.IsInOfferMode"/> = true
    /// </summary>
    protected bool IsInOfferMode(EntityUid? entity, OfferItemComponent? component = null)
    {
        return entity != null && Resolve(entity.Value, ref component, false) && component.IsInOfferMode;
    }
}
