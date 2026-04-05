using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This defines someone or something's scent properties.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Floofstation - network this shit
public sealed partial class ScentComponent : Component
{
    public override bool SendOnlyToOwner => true; // Floofstation - don't bother sending it to others, this is just for the scent editor.

    /// <summary>
    /// The input list of prototypes to load into the scent dictionary.
    /// </summary>
    [DataField("startScents")]
    public List<ProtoId<ScentPrototype>> ScentPrototypesToAdd = new();

    /// <summary>
    /// The actually up to date list of scents.
    /// The actually too instance IDs too!
    /// </summary>
    [ViewVariables]
    [DataField, AutoNetworkedField]
    public List<Scent> Scents = new();
}
