namespace Content.Server._Floof.Language.Components;

[RegisterComponent]
public sealed partial class LanguageRelayComponent : Component
{
    /// <summary>
    ///     Entity whose languages are relayed to this entity.
    /// </summary>
    [DataField]
    public EntityUid RelaySource;
}
