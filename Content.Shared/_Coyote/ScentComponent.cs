using Content.Shared._Floof.Util;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This defines someone or something's scent properties.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Floofstation - network this shit
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
    [DataField, AutoNetworkedField]
    public List<Scent> Scents = new();

    /// <summary>
    ///     Floofstation - how often the client can set scents. Default is 250 ms, well beyond human reaction range, should prevent spam-setting
    /// </summary>
    [DataField(serverOnly: true)]
    public Ticker ScentUpdateDelay = new(TimeSpan.FromMilliseconds(250), true);
}
