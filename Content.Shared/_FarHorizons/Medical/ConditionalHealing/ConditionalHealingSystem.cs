using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Medical;
using Content.Shared.Medical.Healing;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Medical.ConditionalHealing;

public sealed class ConditionalHealingSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly HealingSystem _healing = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!; // Euph
    [Dependency] private readonly SharedVirtualItemSystem _virtualItems = default!; // Euph
    [Dependency] private readonly SharedStackSystem _stacks = default!; // Euph
    [Dependency] private readonly MetaDataSystem _meta = default!; // Euph
    [Dependency] private readonly BlindableSystem _blindables = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Euph - no surgery
        SubscribeLocalEvent<ConditionalHealingComponent, UseInHandEvent>(OnUse/*, before: [typeof(HealingSystem), typeof(SharedSurgerySystem)] */);
        SubscribeLocalEvent<ConditionalHealingComponent, AfterInteractEvent>(OnAfterInteract/*, before: [typeof(HealingSystem), typeof(SharedSurgerySystem)] */);

        SubscribeLocalEvent<ConditionalHealingVirtualItemComponent, HealingDoAfterEvent>(OnHealing, after: [typeof(HealingSystem)]); // Euph
    }

    // Euph - this system has been rewritten to spawn a virtual item in hand and pass it as the healing item instead of creating a fake component
    private void OnUse(Entity<ConditionalHealingComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled ||
            SelectBestMatch((ent, ent.Comp), args.User) is not ConditionalHealingData healing)
            return;

        args.Handled = TryStartHealing(ent, healing, args.User, args.User);
    }

    private void OnAfterInteract(Entity<ConditionalHealingComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            args.Target == null ||
            !_interactionSystem.InRangeUnobstructed(args.User, args.Target.Value, popup: true) ||
            SelectBestMatch((ent, ent.Comp), args.Target.Value) is not ConditionalHealingData healing)
            return;

        args.Handled = TryStartHealing(ent, healing, args.User, args.Target.Value);
    }

    private void OnHealing(Entity<ConditionalHealingVirtualItemComponent> ent, ref HealingDoAfterEvent args)
    {
        // If it's handled, that means the healing has succeeded
        // The healing system doesn't account for eye damage healing so we need to do a little fucking in here
        if (args.Handled && ent.Comp.HealingData is { } healingData)
            _blindables.AdjustEyeDamage(args.Target!.Value, healingData.AdjustEyeDamage);

        // Doesn't give you back the materials even if cancelled, idc
        // This is mostly to avoid leaving the virtual item lingering if the do-after is cancelled
        PredictedQueueDel(ent);
    }

    // Euph - spawns a virtual item in the entity's hand that does the healing
    private bool TryStartHealing(Entity<ConditionalHealingComponent> ent, ConditionalHealingData healing, EntityUid user, EntityUid target)
    {
        // This is awful.
        if (!_virtualItems.TrySpawnVirtualItemInHand(ent, user, dropOthers: false, virtualItem: out var virtItem))
        {
            _popups.PopupClient(Loc.GetString("conditional-healing-needs-hand"), user, user);
            return false;
        }

        _meta.SetEntityName(ent, MetaData(ent).EntityName);
        EnsureComp<TimedDespawnComponent>(virtItem.Value).Lifetime = 10; // Just in case.

        var marker = EnsureComp<ConditionalHealingVirtualItemComponent>(virtItem.Value);
        marker.HealingData = healing;

        var healingComp = healing.MakeComponent();
        AddComp(virtItem.Value, healingComp, overwrite: true);

        if (!_healing.TryHeal((virtItem.Value, healingComp), user, target))
        {
            PredictedQueueDel(virtItem);
            return false;
        }

        // Plagiarising this from the healing system.
        if (TryComp<StackComponent>(ent, out var stackComp))
        {
            if (!_stacks.TryUse((ent.Owner, stackComp), 1))
                return false;
        }
        else
        {
            PredictedQueueDel(ent.Owner);
        }

        return true;
    }

    public ConditionalHealingData? SelectBestMatch(Entity<ConditionalHealingComponent?> item, EntityUid target) =>
        !Resolve(item, ref item.Comp, false)
            ? null
            : item.Comp.HealingDefinitions
                .Where(p => _tag.HasAnyTag(target, p.AllowedTags))
                .Select(p => (ConditionalHealingData?)p.Healing)
                .FirstOrDefault((ConditionalHealingData?)null);
}

// Euph. Mostly a marker component.
[RegisterComponent]
public sealed partial class ConditionalHealingVirtualItemComponent : Component
{
    public ConditionalHealingData? HealingData;
}
