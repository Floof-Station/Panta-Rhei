using Content.Shared._Coyote.SniffAndSmell;
using Content.Shared.EntityEffects;

namespace Content.Shared._Euphoria.EntityEffects.Effects;

public sealed partial class PerfumeEntityEffect : EntityEffectSystem<MetaDataComponent, ApplyScentEffect>
{
    [Dependency]
    private readonly ScentSystem _scentSystem = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ApplyScentEffect> args)
    {
        // we ensure scent comp here because we want to be able to apply scents to players that don't have any selected
        // popuponuses whitelist/blacklist can handle what we can and cant use this on
        var scent = EnsureComp<ScentComponent>(entity);

        scent.Scents.Clear();

        _scentSystem.AddScentPrototype((entity.Owner, scent), args.Effect.Scent);
    }
}

public sealed partial class ApplyScentEffect : EntityEffectBase<ApplyScentEffect>
{
    [DataField(required: true)]
    public string Scent = string.Empty;
}

