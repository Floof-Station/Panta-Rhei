namespace Content.Server._Floof.Language.Components;

/// <summary>
///     Added to an entity when their languages are being relayed.
/// </summary>
[RegisterComponent]
public sealed partial class LanguageRelaySourceComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Relays = new();
}
