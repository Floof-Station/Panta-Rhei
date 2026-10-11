using Content.Shared.Preferences;
using YamlDotNet.RepresentationModel;

namespace Content.Shared._Floof.Humanoid;

public sealed class ProfileMigrationContext(YamlNode profileYaml, YamlNode extractedNode, HumanoidCharacterProfile profile)
{
    public YamlNode ProfileYaml = profileYaml;

    /// <summary>
    ///     Node extracted according to the migrations path.
    /// </summary>
    public YamlNode ExtractedNode = extractedNode;
    public HumanoidCharacterProfile Profile = profile;
}
