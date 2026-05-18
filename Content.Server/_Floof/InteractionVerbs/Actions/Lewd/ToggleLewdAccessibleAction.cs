using Content.Shared._Floof.InteractionVerbs;
using Content.Shared._Floof.Lewd.Components;

namespace Content.Server._Floof.InteractionVerbs.Actions.Lewd;

/// <summary>
///     Toggles whether others' and the user can interact with their lewd organs through clothing.
/// </summary>
public sealed partial class ToggleLewdAccessBypassAction : InteractionAction
{
    public override bool IsAllowed(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps) =>
        deps.EntMan.HasComponent<LewdMobDataComponent>(args.Target);

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps) =>
        IsAllowed(args, proto, deps);

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        if (!deps.TryComp<LewdMobDataComponent>(args.Target, out var lewd))
            return false;

        lewd.BypassClothingChecks = !lewd.BypassClothingChecks;
        deps.EntMan.Dirty(args.Target, lewd);
        return true;
    }
}
