using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._HL.Brainwashing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BrainwashedComponent : Component
{
    public bool ViewingCompulsions = false;

    [DataField, ViewVariables, AutoNetworkedField]
    public List<string> Compulsions = [];

    [DataField]
    public EntProtoId ActionPrototype = "ActionToggleCompulsionsMenu";

    [DataField]
    public EntityUid? Action;
}
