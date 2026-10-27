using System.Linq;
using Content.Shared._DV.Traits.Effects;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Traits.Effects;

/// <summary>
/// Removes every stomach organ from the entity's organs and gives
/// them a specified stomach from a prototype as a replacement.
/// </summary>
public sealed partial class ReplaceStomachEffect : BaseTraitEffect
{
    [DataField("stomachProto", required: true)]
    public EntProtoId<StomachComponent> StomachProto;

    public override void Apply(TraitEffectContext ctx)
    {
        var containerSys = ctx.EntMan.System<SharedContainerSystem>();

        // Make sure both the body component exists and its organ container exists.
        if (!ctx.EntMan.TryGetComponent<BodyComponent>(ctx.Player, out var bodyComp)
            || bodyComp.Organs is not { } organs)
            return;

        // Note: copying the contained entities list before iterating over it, because this loop can mutate it
        foreach (var organ in organs.ContainedEntities.ToList())
        {
            if (!ctx.EntMan.TryGetComponent<StomachComponent>(organ, out var stomachComp))
                continue;

            // STEAL THEIR FUCKING STOMACHS EHEHEHEEE!!
            containerSys.Remove(organ, bodyComp.Organs);
            ctx.EntMan.QueueDeleteEntity(organ);

            // Give them a new one :3
            var newStomach = ctx.EntMan.Spawn(StomachProto);
            containerSys.Insert(newStomach, bodyComp.Organs);
        }
    }
}
