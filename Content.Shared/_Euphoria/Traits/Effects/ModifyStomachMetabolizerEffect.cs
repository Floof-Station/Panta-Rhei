using System.Linq;
using Content.Shared._DV.Traits.Effects;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Metabolism;

namespace Content.Shared._Euphoria.Traits.Effects;

/// <summary>
/// Either replaces or adds new MetabolizerTypes to the <see cref="MetabolizerComponent"/>
/// of all organs on the entity's <see cref="BodyComponent"/> that have one AND a
/// <see cref="StomachComponent"/>.
/// </summary>
public sealed partial class ModifyStomachMetabolizerEffect : BaseTraitEffect
{
    [DataField("newMetabolizerTypes", required: true)]
    public HashSet<ProtoId<MetabolizerTypePrototype>>? NewMetabolizerTypes;

    [DataField("replace", required: true)]
    public bool Replace;

    public override void Apply(TraitEffectContext ctx)
    {
        var _metabolizerSystem = ctx.EntMan.System<MetabolizerSystem>();

        // Make sure both the body component exists and its organ container exists.
        if (!ctx.EntMan.TryGetComponent<BodyComponent>(ctx.Player, out var bodyComp)
            || bodyComp.Organs is not { } organs)
            return;

        // Get all organs with both MetabolizerComponent and StomachComponent.
        // We gotta specify StomachComponent too because lung organs also use MetabolizerComponent.
        var metabolizerStomachOrgans = bodyComp.Organs.ContainedEntities
                .Select(p => ctx.EntMan.TryGetComponent<MetabolizerComponent>(p, out var organ) ? (Entity<MetabolizerComponent>?)(p, organ) : null)
                .Where(p => p != null)
                .Where(p => ctx.EntMan.TryGetComponent<StomachComponent>(p, out var stomachOrgan))
                .ToList();

        foreach (var organ in metabolizerStomachOrgans)
        {
            // Get the MetabolizerComponent, and if it somehow doesn't exist (not actually possible),
            // then go on to the next organ.
            if (!ctx.EntMan.TryGetComponent<MetabolizerComponent>(organ, out var metComp))
                 continue;

            if (NewMetabolizerTypes is not { } editableNewMetabolizerTypes || metComp is not { })
                continue;

            /*
            Even if we're supposed to be merging rather than replacing,
            there's no point in combining it with a null set.

            Also, if someone decided to make a species where the lungs and
            stomach are the same organ, we can't straight up replace
            all their metabolisms or they will stop being able to benefit
            from breathing, which is bad.

            AFAIK, such a species doesn't exist right now on any fork
            and probably never will, but I don't want anyone to have to
            fix this if I'm wrong about that. */
            if ((!Replace || ctx.EntMan.TryGetComponent<LungComponent>(organ, out var lungComp)) &&
                metComp.MetabolizerTypes is { } originalMetabolizerTypes)
                editableNewMetabolizerTypes.UnionWith(originalMetabolizerTypes);

            _metabolizerSystem.SetMetabolizerTypes(metComp, editableNewMetabolizerTypes);
        }
    }
}
