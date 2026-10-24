using Content.Shared._Floof.Util;

namespace Content.Shared._Floof.Examine;

public sealed class CustomExaminePreset : ICVarSerializable
{
    public string Name;
    // We don't preserve anything but content because it's too complex to do so and frankly i dont think anyone will care
    public string? PublicContent, SubtleContent;

    // For ICvarSerializable
    public CustomExaminePreset()
    {
        Name = string.Empty;
    }

    public CustomExaminePreset(string name, string? publicContent, string? subtleContent)
    {
        publicContent = publicContent?.Trim();
        subtleContent = subtleContent?.Trim();

        Name = name;
        PublicContent = string.IsNullOrEmpty(publicContent) ? null : publicContent;
        SubtleContent = string.IsNullOrEmpty(subtleContent) ? null : subtleContent;
    }

    public static CustomExaminePreset CreateEmpty() => new CustomExaminePreset();

    public IEnumerable<string> SerializeFields() => new string[] { Name, PublicContent ?? string.Empty, SubtleContent ?? string.Empty };

    public void DeserializeFields(string[] fields)
    {
        if (fields.Length != 3)
        {
            Name = $"<invalid: {string.Join(", ", fields)}>";
            return;
        }

        Name = fields[0];
        PublicContent = fields[1];
        SubtleContent = fields[2];
    }
}
