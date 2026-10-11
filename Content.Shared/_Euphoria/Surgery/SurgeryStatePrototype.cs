using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Surgery;

//pair this down to just the ID
//Make a new component/entity that contains the information of state and organ
[Prototype("surgeryState")]
public sealed partial class SurgeryStatePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

}
