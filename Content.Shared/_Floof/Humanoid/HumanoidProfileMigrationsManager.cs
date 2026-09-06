using System.Text.RegularExpressions;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

// ReSharper disable BadExpressionBracesLineBreaks

namespace Content.Shared._Floof.Humanoid;

/// <summary>
///     Handles migrating old character profiles (EE Floofstation) to the new format (Project Panta-rhei/Euphoria Station)
/// </summary>
public sealed class HumanoidProfileMigrationsManager : IHumanoidProfileMigrationsManager
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    /// <summary>
    ///     <p>Dictionary of simple migrations in the form of "old field path"->"setter function", filled in <see cref="Initialize"/>. </p>
    ///
    ///     <p>Example of a field path: /profile/_traitPreferences[0]. This takes the root yaml data node,
    ///     assumes it's a mapping (dict), takes the value at key "profile", assumes it's another mapping (dict), takes the value at its key "_traitPreferences",
    ///     assumes it's a sequence (list), takes the value at index 0, and calls the setter function with that value.</p>
    ///
    ///     <p>This would match the following yaml:</p>
    ///     <code>
    ///     profile:
    ///       height: 0.5
    ///       _traitPreferences:
    ///       - A # --- this value would get picked and passed to the setter function
    ///       - B
    ///       - C
    ///     </code>
    ///
    ///
    ///     <p>If the yaml doesn't contain the specified path, the setter function is not called.</p>
    /// </summary>
    private Dictionary<List<YamlPathParser.Part>, Action<ProfileMigrationContext>> _simpleMigrations = new();

    // Species migrations (old -> new)
    Dictionary<string, ProtoId<SpeciesPrototype>> _speciesMigrationMap = new()
    {
        { "Shadowkin", "Shadekin" }, // 2026-08-30 - EE shadowkin replaced with Starlight shadekin
    };

    private ISawmill Log { get => field ??= Logger.GetSawmill("profile.migrations");  }

    public HumanoidProfileMigrationsManager()
    {
        _simpleMigrations.Clear();

        // Height was renamed
        AddMigration("/profile/height", ctx => { ctx.Profile.Height = ctx.ExtractedNode.AsFloat(); });

        // During the loadouts rework, trait preferences were changed from simple ProtoIds to "{Prototype: <id>}" strings with plans to extend the format.
        // This only affects SOME profiles, but not all of them.
        AddMigration("/profile/_traitPreferences", ctx => {
            if (ctx.ExtractedNode is not YamlSequenceNode sequence)
                return;

            var regex = new Regex(@"^\{Prototype: ([a-zA-Z0-9_]+).*\}$");
            foreach (var node in sequence)
            {
                if (node is not YamlScalarNode { Value: {} value })
                    continue;

                var match = regex.Match(value);
                if (match.Success)
                    ctx.Profile = ctx.Profile.WithTraitPreference(match.Groups[1].Value, _protoMan);
            }
        });

        // Species migrations
        AddMigration("/profile/species", ctx =>
        {
            if (_speciesMigrationMap.TryGetValue(ctx.Profile.Species, out var replacementSpecies))
                ctx.Profile.Species = replacementSpecies;
        });
    }

    public void MigrateProfile(YamlNode profileYaml, HumanoidCharacterProfile profile)
    {
        foreach (var (path, action) in _simpleMigrations)
        {
            try
            {
                var value = GetValueOrNull(profileYaml, path);
                if (value is null)
                    continue;

                action.Invoke(new ProfileMigrationContext(profileYaml, value, profile));
            }
            catch (Exception e)
            {
                Log.Error($"Cannot apply migration on {path}: {e}");
            }
        }
    }

    public void AddMigration(string path, Action<ProfileMigrationContext> action)
    {
        _simpleMigrations.Add(new YamlPathParser(path).Parse(), action);
    }

    public YamlNode? GetValueOrNull(YamlNode root, List<YamlPathParser.Part> path)
    {
        var curr = root;
        foreach (var part in path)
        {
            curr = part.Resolve(curr);
            if (curr is null)
                break;
        }

        return curr;
    }

    public YamlNode? GetValueOrNull(YamlNode root, string path) =>
        GetValueOrNull(root, new YamlPathParser(path).Parse());
}
