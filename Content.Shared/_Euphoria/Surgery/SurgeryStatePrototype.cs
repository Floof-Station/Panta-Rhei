using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Surgery;

[Prototype("surgeryState")]
public sealed partial class SurgeryStatePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public bool State = false;

    [DataField]
    public EntityUid Organ = default;

}
