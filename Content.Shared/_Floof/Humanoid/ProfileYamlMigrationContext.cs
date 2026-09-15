using YamlDotNet.RepresentationModel;

namespace Content.Shared._Floof.Humanoid;

public sealed class ProfileYamlMigrationContext(YamlNode profileYaml, YamlNode extractedNode)
{
    public YamlNode ProfileYaml = profileYaml;

    /// <summary>
    ///     Node extracted according to the migrations path.
    /// </summary>
    public YamlNode ExtractedNode = extractedNode;

    /// <summary>
    ///     What to write in place of the extracted node. By default, equals the node that was just read.
    /// </summary>
    public YamlNode? ToWrite = extractedNode;
}
