namespace Content.Server._Floof.Language.Components;

/// <summary>
///     The first entity placed inside its container slot with the matching name will relay its languages to this entity.
/// </summary>
[RegisterComponent]
public sealed partial class ContainerLanguageRelayComponent : Component
{
    [DataField(required: true)]
    public string ContainerName;
}
