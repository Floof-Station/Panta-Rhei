using Content.Shared._Euphoria.MagicalCommand;
using Content.Shared._Euphoria.Tools.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind.Components;
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
        // SubscribeLocalEvent<AttunedEntityComponent, MindRemovedMessage>(OnMindRemoved);
    }

    // private void OnMindRemoved(Entity<AttunedEntityComponent> ent, ref MindRemovedMessage message)
    // {
    //     Log.Debug("who up doinking it");
    // }

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
        Log.Debug(HasComp<AttunableEquipmentComponent>(target).ToString());
        EnsureComp<AttunableEquipmentComponent>(target, out var attunedComp);
        if (original.Comp.AttunedEnt != null)
            EnsureComp<AttunedEntityComponent>(original.Comp.AttunedEnt.Value);
        var temp = original.Comp.AttunedEnt;
        if (temp != null)
            Log.Debug(temp.Value.Id.ToString());
        attunedComp.AttunedEnt = original.Comp.AttunedEnt;
        if (attunedComp.AttunedEnt != null)
            Log.Debug(attunedComp.AttunedEnt.Value.ToString());
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
                EnsureComp<AttunedEntityComponent>(user);
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
