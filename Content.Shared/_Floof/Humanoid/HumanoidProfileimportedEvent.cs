using Content.Shared.Humanoid;
using YamlDotNet.RepresentationModel;

namespace Content.Shared._Floof.Humanoid;

public sealed class HumanoidProfileImportedEvent
{
    /// <summary>
    ///     The profile as it was deserialized.
    /// </summary>
    public HumanoidProfileExport DeserializedProfile;

    /// <summary>
    ///     The raw structure of the yaml file that contained this profile.
    /// </summary>
    public YamlNode ProfileYaml;

    public HumanoidProfileImportedEvent(HumanoidProfileExport deserializedProfile, YamlNode profileYaml)
    {
        DeserializedProfile = deserializedProfile;
        ProfileYaml = profileYaml;
    }
}
