namespace Content.Shared._Euphoria.Selector.Events;

public abstract class BaseConfigurationSelectedEvent(
    EntityConfigurationComponent.GroupDefinition group,
    string value,
    Dictionary<string, string> currentConfig,
    EntityUid? user) : EntityEventArgs
{
    public EntityConfigurationComponent.GroupDefinition Group = group;
    public string Value = value;

    public Dictionary<string, string> CurrentConfig = currentConfig;

    /// <summary>
    ///     The entity performing the selection.
    /// </summary>
    public EntityUid? User = user;

    /// <summary>
    ///     Returns the changed value as a int
    /// </summary>
    public int GetValueAsInt() => int.Parse(Value);

    /// <summary>
    ///     Returns the changed value as a float
    /// </summary>
    public float GetValueAsFloat() => float.Parse(Value);

    /// <summary>
    ///     Returns the value of the config group with the specified id.
    /// </summary>
    public string GetCurrentConfig(string id) => CurrentConfig[id];

    /// <summary>
    ///     Returns the value of the config group with the specified id, parsed as a float.
    /// </summary>
    public float GetCurrentConfigFloat(string id) => float.Parse(CurrentConfig[id]);
}
