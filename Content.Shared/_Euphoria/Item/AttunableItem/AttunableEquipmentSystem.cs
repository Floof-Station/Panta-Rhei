using Content.Shared._Euphoria.MagicalCommand;
using Content.Shared._Euphoria.Tools.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;

namespace Content.Shared._Euphoria.Item.AttunableItem;

/// <summary>
/// Makes it so that an item cannot be equipped until it is 'attuned' to, effectively meaning it can only have one user.
/// </summary>
public sealed class AttunableEquipmentSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttunableEquipmentComponent, PostCorruptingEvent>(OnPostCorruption);
        SubscribeLocalEvent<AttunableEquipmentComponent, FriendshipTransformEvent>(OnFriendshipTransform);
        SubscribeLocalEvent<AttunableEquipmentComponent, BeingEquippedAttemptEvent>(OnEquippedAttempt);
        SubscribeLocalEvent<AttunableEquipmentComponent, GetVerbsEvent<AlternativeVerb>>(AddVerb);
    }

    private void OnPostCorruption(Entity<AttunableEquipmentComponent> ent, ref PostCorruptingEvent args)
    {
        TransferAttunableEquipment(args.CreatedItem, ent);
    }

    private void OnFriendshipTransform(Entity<AttunableEquipmentComponent> ent, ref FriendshipTransformEvent args)
    {
        TransferAttunableEquipment(args.CreatedEnt, ent);
    }

    /// <summary>
    /// Used for processes similar to polymorphing.
    /// </summary>
    private void TransferAttunableEquipment(EntityUid target, Entity<AttunableEquipmentComponent> original)
    {
        if (!HasComp<ClothingComponent>(target))
            return;
        EnsureComp<AttunableEquipmentComponent>(target, out var attunedComp);
        attunedComp.AttunedEnt = original.Comp.AttunedEnt;
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
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.AttunedEnt != null)
            return;

        var user = args.User;
        AlternativeVerb attuneVerb = new()
        {
            Text = "Attune",
            Message = Loc.GetString("attunable-attune-action-hint"),
            Act = () =>
            {
                ent.Comp.AttunedEnt = user;
            },
        };
        args.Verbs.Add(attuneVerb);
    }
}
