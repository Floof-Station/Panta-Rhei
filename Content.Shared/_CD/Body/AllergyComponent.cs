using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CD.Body;


[Serializable]
[NetSerializable]
[DataDefinition]
public sealed partial class AllergyData
{
    [DataField]
    public FixedPoint2 Intensity = 1;

    [DataField]
    public ProtoId<ReagentPrototype> ReactionReagent = new("Histamine");
}
