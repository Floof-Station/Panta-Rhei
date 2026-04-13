using Content.Shared._Floof.Humanoid.ModifyUndies;
using Content.Shared.Humanoid;

namespace Content.Shared._Floof.InteractionVerbs.Requirements;

/// <summary>
///     Requires the target's underwear to be in the specified state.
///     This is NOT the same as SlotObstructionRequirement as underwear is not clothing-based (guh).
/// </summary>
public sealed partial class UnderwearRequirement : InvertableInteractionRequirement
{
    /// <summary>
    ///     Which humanoid layer to consider. Should only ever be UndergarmentTop or UndergarmentBottom, unless support for more is added to ModifyUndiesComponent/system.
    /// </summary>
    [DataField]
    public HumanoidVisualLayers Layer;

    [DataField]
    public bool CheckUser = false;

    public override bool IsMet(InteractionArgs args,
        InteractionVerbPrototype proto,
        InteractionAction.VerbDependencies deps)
    {
        var sys = deps.System<ModifyUndiesSystem>();
        var target = CheckUser ? args.User : args.Target;
        return sys.IsMissingUndergarment(target, Layer) ^ Inverted;
    }
}
