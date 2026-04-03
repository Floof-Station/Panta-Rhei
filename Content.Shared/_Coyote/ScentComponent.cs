using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This defines someone or something's scent properties.
/// </summary>
[RegisterComponent]
public sealed partial class ScentComponent : Component
{
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
    [DataField]
    public List<Scent> Scents = new();
}
