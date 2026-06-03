using Content.Shared._Floof.Clothing.SlotBlocker;
using Content.Shared._Floof.Humanoid.ModifyUndies;
using Content.Shared._Floof.Lewd.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.InteractionVerbs.Requirements;

/// <summary>
///     Requires the user and the target to consent to lewd interactions, and also one of the following: <br/>
///     a. The target's underwear is down and accessible (no inner clothing, outer clothing is not obstructing inner clothing) <br/>
///     b. The target has toggled the "allow lewd access" toggle. <br/>
/// </summary>
public sealed partial class LewdAccessibleRequirement : InteractionRequirement
{
    [DataField]
    public bool CheckUserUnderwear, CheckTargetUnderwear;

    /// <summary>
    ///     Which humanoid layer to consider for underwear. See UnderwearRequirement.
    /// </summary>
    [DataField]
    public HumanoidVisualLayers LayerUser, LayerTarget;

    /// <summary>
    ///     Virtual item used to check whether the relevant slots are accessible.
    /// </summary>
    [DataField]
    public EntProtoId VirtItemUser = "VirtualClothingPanties", VirtItemTarget = "VirtualClothingPanties";

    [DataField]
    public SlotFlags UserSlot = SlotFlags.INNERCLOTHING, TargetSlot = SlotFlags.INNERCLOTHING;

    public override bool IsMet(InteractionArgs args,
        InteractionVerbPrototype proto,
        InteractionAction.VerbDependencies deps)
    {
        if (CheckUserUnderwear && !UnderwearAccessible(args.User, deps, LayerUser, VirtItemUser, UserSlot))
            return false;

        if (CheckTargetUnderwear && !UnderwearAccessible(args.Target, deps, LayerTarget, VirtItemTarget, TargetSlot))
            return false;

        return true;
    }

    private bool UnderwearAccessible(EntityUid mob, InteractionAction.VerbDependencies deps, HumanoidVisualLayers layer, EntProtoId virtItem, SlotFlags slot)
    {
        if (deps.TryComp<LewdMobDataComponent>(mob, out var lewd) && lewd.BypassClothingChecks)
            return true;

        var underwearSystem = deps.System<ModifyUndiesSystem>();
        if (!underwearSystem.IsMissingUndergarment(mob, layer))
            return false;

        // This is the most expensive part
        var blockerSystem = deps.System<SlotBlockerSystem>();
        if (blockerSystem.IsSlotObstructedOrOccupied(mob, virtItem, SlotBlockerSystem.CheckType.Equip, slot, out _))
            return false;

        return true;
    }
}
