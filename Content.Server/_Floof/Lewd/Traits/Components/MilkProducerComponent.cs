using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Floof.Lewd.Traits.Components;

[RegisterComponent, Access(typeof(LewdTraitSystem))]
public sealed partial class MilkProducerComponent : Component
{
    [DataField("solutionname")]
    public string SolutionName = "breasts";

    [DataField]
    public ProtoId<ReagentPrototype> ReagentId = "Milk";

    [DataField]
    public FixedPoint2 MaxVolume = FixedPoint2.New(200);

    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    [DataField]
    public FixedPoint2 QuantityPerUpdate = 10;

    [DataField]
    public float HungerUsage = 5f;

    [DataField]
    public TimeSpan GrowthDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
}
