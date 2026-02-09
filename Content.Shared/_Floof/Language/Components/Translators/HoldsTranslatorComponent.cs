namespace Content.Shared._Floof.Language.Components.Translators;

/// <summary>
///     Applied internally to the holder of an entity with [HandheldTranslatorComponent].
/// </summary>
[RegisterComponent]
public sealed partial class HoldsTranslatorComponent : Component
{
    [NonSerialized, ViewVariables]
    public HashSet<Entity<HandheldTranslatorComponent>> Translators = new();
}
