namespace Content.Shared._Euphoria.Selector.Events;

/// <summary>
///     Raised on an entity with <see cref="EntityConfigurationComponent"/> when the player makes a selection.
/// </summary>
public sealed class ConfigurationSelectedEvent(
    EntityConfigurationComponent.GroupDefinition group,
    string value,
    Dictionary<string, string> currentConfig,
    EntityUid? user
) : BaseConfigurationSelectedEvent(group, value, currentConfig, user);
