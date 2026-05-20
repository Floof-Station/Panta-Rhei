using Content.Shared._Common.Consent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.InteractionVerbs.Requirements;

/// <summary>
///     Requires the target to consent to the action.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ConsentRequirement : InvertableInteractionRequirement
{
    [DataField]
    public ProtoId<ConsentTogglePrototype> Consent;

    /// <summary>
    ///      Whether to bypass the consent checks when interacting with yourself.
    /// </summary>
    [DataField]
    public bool BypassSelf = true;

    public override bool IsMet(InteractionArgs args, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        if (BypassSelf && args.User == args.Target)
            return true;

        var consent = deps.System<SharedConsentSystem>();
        return consent.HasConsent(args.Target, Consent) ^ Inverted;
    }
}
