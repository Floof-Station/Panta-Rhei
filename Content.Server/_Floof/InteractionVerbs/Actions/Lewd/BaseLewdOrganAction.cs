using Content.Shared._Floof.InteractionVerbs;
using Content.Shared.FixedPoint;

namespace Content.Server._Floof.InteractionVerbs.Actions.Lewd;

public abstract partial class BaseLewdOrganAction : InteractionAction
{
    [DataField]
    public FixedPoint2 MaxAmount = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 MinRepeatAmount = FixedPoint2.FromCents(50);

    public override bool IsAllowed(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps) => CanPerform(args, proto, true, deps);
}
