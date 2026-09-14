using Content.Shared.Preferences;
using YamlDotNet.RepresentationModel;

namespace Content.Shared._Floof.Humanoid;

public interface IHumanoidProfileMigrationsManager
{
    /// <summary>
    ///     Migrates a profile using predefined migrations.
    /// </summary>
    public void MigrateProfile(YamlNode profileYaml, HumanoidCharacterProfile profile);

    /// <summary>
    ///     Adds a new simple migration to be executed whenever a profile is being imported.
    /// </summary>
    public void AddMigration(string path, Action<ProfileMigrationContext> action);

    /// <summary>
    ///     Tries to retrieve the value at the specified path in the YAML node. Can throw ArgumentException if the path is invalid.
    /// </summary>
    /// <param name="root">The root YAML node.</param>
    /// <param name="path">The path to the value. For example, /dict1/innerdict/evenMoreInnerDict, or [1]/components[1]/someField.</param>
    public YamlNode? GetValueOrNull(YamlNode root, List<YamlPathParser.Part> path);

    /// <summary>
    ///     Tries to retrieve the value at the specified path in the YAML node. Can throw ArgumentException if the path is invalid.
    /// </summary>
    /// <param name="root">The root YAML node.</param>
    /// <param name="path">The path to the value. For example, /dict1/innerdict/evenMoreInnerDict, or [1]/components[1]/someField.</param>
    public YamlNode? GetValueOrNull(YamlNode root, string path);
}
