using Content.Shared._Common.Consent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.InteractionVerbs.Requirements;

/// <summary>
///     Requires the verb to be fascillated by a specific entity in the interaction, e.g. the user, the target, etc.
///     Do NOT use this requirement if you just want to limit the verb to the target or user - set the <see cref="InteractionVerbPrototype.AllowedSource"/> field instead!
/// </summary>
public sealed partial class SourceRequirement : InvertableInteractionRequirement
{
    [DataField]
    public InteractionVerbSource Sources = InteractionVerbSource.All;

    public override bool IsMet(InteractionArgs args, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        return (Sources.HasFlag(args.Source) ^ Inverted);
    }
}
