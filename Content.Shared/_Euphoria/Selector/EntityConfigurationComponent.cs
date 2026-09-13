using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Euphoria.Selector;

/// <summary>
///     When applied to an entity, shows groups of verbs that allow to configure it.
///
///     <p>When the player selects a value, an event is raised. Values are represented as strings.</p>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityConfigurationComponent : Component
{
    /// <summary>
    ///     Map of (group id to chosen value).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> CurrentConfig = new();

    /// <summary>
    ///     Map of (group id to available configs).
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, GroupDefinition> ConfigGroups;

    [DataField]
    public bool ShowExamine = true;

    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class GroupDefinition
    {
        /// <summary>
        ///     L10N string to show as the name of the associated verb group.
        ///     The loc string receives a {$value} parameter which contains the current value of the config group.
        /// </summary>
        [DataField(required: true)]
        public LocId Name;

        /// <summary>
        ///     If set, this is displayed instead of <see cref="Name"/> in the examine window. If not set, Name is used instead.
        /// </summary>
        [DataField]
        public LocId? ExamineName;

        /// <summary>
        ///     Message to show when a config option is chosen. Receives {$groupId} and {$value} as parameters.
        /// </summary>
        [DataField]
        public LocId SelectedPopup = "verb-selector-chosen-popup";

        // Internal id in the ConfigGroups dict, set on map init
        [DataField]
        public string Id = string.Empty;

        [DataField]
        public string Icon = "/Textures/_Floof/Interface/VerbIcons/resize.svg.192dpi.png";

        [DataField(required: true)]
        public ConfigOption[] Options = default!;
    }

    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class ConfigOption
    {
        /// <summary>
        ///     L10N string to show as the name of the value selector.
        /// The loc string receives a {$value} parameter which contains the
        /// </summary>
        [DataField(required: true)]
        public LocId Name;

        [DataField(required: true)]
        public string Value = string.Empty;
    }
}
