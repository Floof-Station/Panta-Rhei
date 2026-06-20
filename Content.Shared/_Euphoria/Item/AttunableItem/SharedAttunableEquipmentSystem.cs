using Content.Shared._Euphoria.MagicalCommand;
using Content.Shared._Euphoria.MagicalCommand.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Serilog;

namespace Content.Shared._Euphoria.Item.AttunableItem;

/// <summary>
/// Makes it so that an item cannot be equipped until it is 'attuned' to, effectively meaning it can only have one user.
/// </summary>
public abstract partial class SharedAttunableEquipmentSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttunableEquipmentComponent, PostCorruptingEvent>(OnPostCorruption);
        SubscribeLocalEvent<AttunableEquipmentComponent, FriendshipTransformEvent>(OnFriendshipTransform);
        SubscribeLocalEvent<AttunableEquipmentComponent, BeingEquippedAttemptEvent>(OnEquippedAttempt);
        SubscribeLocalEvent<AttunableEquipmentComponent, GetVerbsEvent<AlternativeVerb>>(AddVerb);
    }

    protected virtual void OnPostCorruption(Entity<AttunableEquipmentComponent> ent, ref PostCorruptingEvent args)
    {
        TransferAttunableEquipment(args.CreatedItem, ent);
    }

    private void OnFriendshipTransform(Entity<AttunableEquipmentComponent> ent, ref FriendshipTransformEvent args)
    {
        TransferAttunableEquipment(args.CreatedEnt, ent);
    }

    /// <summary>
    /// Used for processes similar to polymorphing.
    /// Gives the new object AttunableEquipment and binds the old user to the new object.
    /// </summary>
    private void TransferAttunableEquipment(EntityUid target, Entity<AttunableEquipmentComponent> original)
    {
        if (!HasComp<ClothingComponent>(target))
            return;
        EnsureComp<AttunableEquipmentComponent>(target, out var attunedComp);
        if (original.Comp.AttunedEnt == null)
            return;
        EnsureComp<AttunedEntityComponent>(original.Comp.AttunedEnt.Value, out var attunerComp);
        attunerComp.AttunedTo = target;
        attunedComp.AttunedEnt = original.Comp.AttunedEnt;
        Dirty(target, attunedComp);
    }

    private void OnEquippedAttempt(Entity<AttunableEquipmentComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (ent.Comp.AttunedEnt != args.EquipTarget)
        {
            args.Reason = Loc.GetString("attunable-equip-fail");
            args.Cancel();
        }
    }

    private void AddVerb(Entity<AttunableEquipmentComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.AttunedEnt != null ||
            HasComp<AttunedEntityComponent>(args.User))
            return;

        var user = args.User;
        AlternativeVerb attuneVerb = new()
        {
            Text = "Attune",
            Message = Loc.GetString("attunable-attune-action-hint"),
            Act = () =>
            {
                ent.Comp.AttunedEnt = user;
                EnsureComp<AttunedEntityComponent>(user, out var attunerComp);
                attunerComp.AttunedTo = ent.Owner;
                var ev = new AttunedToEvent
                {
                    Attunable = ent,
                    Attuner = user,
                };
                RaiseLocalEvent(ent.Owner, ev);
            },
        };
        args.Verbs.Add(attuneVerb);
    }
}

/// <summary>
/// Raised on the attunable entity when something attunes to it.
/// </summary>
public sealed partial class AttunedToEvent : EntityEventArgs
{
    public EntityUid Attunable;
    public EntityUid Attuner;
}
