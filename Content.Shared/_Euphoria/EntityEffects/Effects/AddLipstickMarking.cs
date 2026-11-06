using System.Linq;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.EntityEffects.Effects;

public sealed partial class AddLipstickMarking : EntityEffectSystem<VisualBodyComponent , AddFaceMarkingEffect>
{
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    protected override void Effect(Entity<VisualBodyComponent > entity, ref EntityEffectEvent<AddFaceMarkingEffect> args)
    {
        var effect = args.Effect;

        if (!TryComp<VisualBodyComponent>(entity.Owner, out var visualBody))
            return;

        if (!_visualBody.TryGatherMarkingsData(
                (entity.Owner, visualBody),
                null,
                out _,
                out _,
                out var applied))
            return;

        if (!applied.TryGetValue(effect.Organ, out var organMarkings))
            return;

        if (!organMarkings.TryGetValue(effect.Layer, out var markings))
        {
            markings = new List<Marking>();
            organMarkings[effect.Layer] = markings;
        }

        if (effect.Remove)
        {
            var removed = markings.RemoveAll(marking => marking.MarkingId == effect.Marking);

            if (removed == 0)
                return;
        }
        else
        {
            if (markings.Any(marking => marking.MarkingId == effect.Marking))
                return;

            markings.Add(new Marking(effect.Marking, new List<Color> { effect.Color }));
        }

        _visualBody.ApplyMarkings(entity.Owner, applied);
    }
}

public sealed partial class AddFaceMarkingEffect : EntityEffectBase<AddFaceMarkingEffect>
{
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public ProtoId<OrganCategoryPrototype> Organ = "Head";

    [DataField]
    public HumanoidVisualLayers Layer = HumanoidVisualLayers.Face;

    [DataField]
    public bool Remove;
}

