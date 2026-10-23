using System.Linq;
using Content.Shared._DV.Traits.Effects;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Whitelist;
using Content.Shared.Body.Systems;

namespace Content.Shared._Euphoria.Traits.Effects;

/// <summary>
/// Allows for the setting of the fields concerning the special digestible
/// whitelist on <see cref="StomachComponent"/>. Sets these fields on all such organs in the body.
/// </summary>
public sealed partial class ModifySpecialDigestibleWhitelists : BaseTraitEffect
{
    [DataField("newIsSpecialDigestibleExclusive")]
    public bool? NewIsSpecialDigestibleExclusive;

    [DataField("newSpecialDigestible")]
    public EntityWhitelist? NewSpecialDigestible;

    public override void Apply(TraitEffectContext ctx)
    {
        var _stomachSystem = ctx.EntMan.System<StomachSystem>();

        // Make sure both the body component exists and its organ container exists.
        if (!ctx.EntMan.TryGetComponent<BodyComponent>(ctx.Player, out var bodyComp)
            || bodyComp.Organs is not { } organs)
            return;

        // Get all organs with StomachComponent.
        var stomachOrgans = bodyComp.Organs.ContainedEntities
                .Select(p => ctx.EntMan.TryGetComponent<StomachComponent>(p, out var organ) ? (Entity<StomachComponent>?)(p, organ) : null)
                .Where(p => p != null)
                .ToList();

        foreach (var organ in stomachOrgans)
        {
            if (organ is not { } organNotNull || organNotNull.Comp is not { } stoComp)
                continue;

            // Set specialDigestible if one was specified
            if (NewSpecialDigestible is { })
                _stomachSystem.SetSpecialDigestible(stoComp, NewSpecialDigestible);

            // Set isSpecialDigestibleExclusive if one was specified.
            if (NewIsSpecialDigestibleExclusive is { })
                _stomachSystem.SetIsSpecialDigestibleExclusive(stoComp, NewIsSpecialDigestibleExclusive.Value);
        }
    }
}
