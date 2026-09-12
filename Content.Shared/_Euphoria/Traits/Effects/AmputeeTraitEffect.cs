using System.Linq;
using Content.Shared._DV.Traits.Effects;
using Content.Shared.Body.Components;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Shared._Euphoria.Traits.Effects;

public sealed partial class AmputeeTraitEffect : BaseTraitEffect
{
    [DataField("removeLimb", required: true)]
    public string RemoveLimb;

    [DataField("replaceLimb")]
    public string ReplaceLimb;

    public override void Apply(TraitEffectContext ctx)
    {
        var container = ctx.EntMan.System<SharedContainerSystem>();

        if (ctx.EntMan.TryGetComponent<BodyComponent>(ctx.Player, out var bodyComp) && bodyComp.Organs != null)
        {
            var organsToRemove = bodyComp.Organs.ContainedEntities
                .Select(p => ctx.EntMan.TryGetComponent<OrganComponent>(p, out var organ) ? (Entity<OrganComponent>?)(p, organ) : null)
                .Where(p => p != null)
                .Where(p => new ProtoId<OrganCategoryPrototype>(RemoveLimb) == p!.Value.Comp.Category)
                .ToList();

            foreach (var organ in organsToRemove)
            {
                if(organ is {} organNotNullable)
                {
                    container.Remove(organNotNullable.Owner, bodyComp.Organs);
                    ctx.EntMan.DeleteEntity(organ);
                }
            }
        }
    }
}
