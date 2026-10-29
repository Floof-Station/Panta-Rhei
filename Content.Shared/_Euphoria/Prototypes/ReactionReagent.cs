using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Euphoria.Prototypes;

[Prototype("reactionReagentProto")]
public sealed partial class ReactionReagentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<ProtoId<ReagentPrototype>> Reagents = new();
}
