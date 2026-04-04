using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.EntityEffects.Effects;

public sealed partial class CauseLactation : EntityEffectBase<CauseLactation>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-cause-lactation", ("chance", Probability));
}

public sealed partial class RemoveLactation : EntityEffectBase<RemoveLactation>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-remove-lactation", ("chance", Probability));
}
