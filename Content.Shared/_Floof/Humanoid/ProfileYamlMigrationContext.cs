using YamlDotNet.RepresentationModel;

namespace Content.Shared._Floof.Humanoid;

public sealed class ProfileYamlMigrationContext(YamlNode profileYaml, YamlNode extractedNode)
{
    public YamlNode ProfileYaml = profileYaml;

    /// <summary>
    ///     Node extracted according to the migrations path.
    /// </summary>
    public YamlNode ExtractedNode = extractedNode;
}
