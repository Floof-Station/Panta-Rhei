using Content.Server._Floof.Lewd.Traits.Components;
using Content.Shared._Euphoria.EntityEffects.Effects;
using Content.Shared.EntityEffects;

namespace Content.Server._Euphoria.EntityEffects.Effects;


public sealed partial class CauseLactationEntityEffectSystem : EntityEffectSystem<MetaDataComponent, CauseLactation>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<CauseLactation> args)
    {
        if (HasComp<MilkProducerComponent>(entity))
            return;

        EnsureComp<MilkProducerComponent>(entity);
    }
}

public sealed partial class RemoveLactationEntityEffectSystem : EntityEffectSystem<MetaDataComponent, RemoveLactation>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<RemoveLactation> args)
    {
        if (!HasComp<MilkProducerComponent>(entity))
            return;

        RemComp<MilkProducerComponent>(entity);
    }
}
