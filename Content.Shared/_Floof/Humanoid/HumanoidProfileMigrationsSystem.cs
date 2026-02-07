using System.Text.RegularExpressions;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared._Floof.Humanoid;

// ReSharper disable BadExpressionBracesLineBreaks
// ReSharper disable BadEmptyBracesLineBreaks

/// <summary>
///     Handles migrating old character profiles (EE Floofstation) to the new format (Project Panta-rhei/Euphoria Station)
/// </summary>
public sealed class HumanoidProfileMigrationsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    /// <summary>
    ///     Dictionary of simple migrations in the form of "old field path"->"setter function". <br/><br/>
    ///
    ///     Example of a field path: /profile/_traitPreferences[0]. This takes the root yaml data node,
    ///     assumes it's a mapping (dict), takes the value at key "profile", assumes it's another mapping (dict), takes the value at its key "_traitPreferences",
    ///     assumes it's a sequence (list), takes the value at index 0, and calls the setter function with that value.</br></br>
    ///
    ///     If the yaml doesn't contain the specified path, the setter function is not called.
    /// </summary>
    private readonly Dictionary<string, Action<YamlNode, HumanoidProfileExport, HumanoidProfileMigrationsSystem>> _simpleMigrations = new()
    {
        // Height was renamed
        { "/profile/height", (n, p, _) => { p.Profile.Height = n.AsFloat(); } },
        // During the loadouts rework, trait preferences were changed from simple ProtoIds to "{Prototype: <id>}" strings with plans to extend the format.
        // This only affects SOME profiles, but not all of them.
        { "/profile/_traitPreferences", (n, p, ctx) =>
        {
            var sequence = n as YamlSequenceNode;
            if (sequence == null)
                return;

            var regex = new Regex(@"^\{Prototype: ([a-zA-Z0-9_]+).*\}$");
            foreach (var node in sequence)
            {
                if (node is not YamlScalarNode { Value: {} value })
                    continue;

                var match = regex.Match(value);
                if (match.Success)
                    p.Profile = p.Profile.WithTraitPreference(match.Groups[1].Value, ctx._protoMan);
            }
        } },
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<HumanoidProfileImportedEvent>(OnProfileImported);
    }

    private void OnProfileImported(HumanoidProfileImportedEvent ev)
    {
        MigrateProfile(ev.ProfileYaml, ev.DeserializedProfile);
    }

    public void MigrateProfile(YamlNode profileYaml, HumanoidProfileExport profile)
    {
        foreach (var (path, action) in _simpleMigrations)
        {
            try {
                var value = GetValueOrNull(profileYaml, path);
                if (value is null)
                    continue;

                action.Invoke(value, profile, this);
            } catch (Exception e) {
                Log.Error($"Cannot apply migration on {path}: {e}");
            }
        }
    }

    /// <summary>
    ///     Tries to retrieve the value at the specified path in the YAML node. Can throw ArgumentException if the path is invalid.
    /// </summary>
    /// <param name="root">The root YAML node.</param>
    /// <param name="path">The path to the value. For example, /dict1/innerdict/evenMoreInnerDict, or [1]/components[1]/someField.</param>
    public YamlNode? GetValueOrNull(YamlNode root, string path)
    {
        var curr = root;
        var parts = new YamlPathParser(path).Parse();
        foreach (var part in parts)
        {
            curr = part.Resolve(curr);
            if (curr is null)
                break;
        }

        return curr;
    }
}

// Yes I overengineered it
public sealed class YamlPathParser(string input)
{
    private readonly List<Part> _parts = new();
    private int _index = 0;

    public List<Part> Parse()
    {
        _index = 0;
        _parts.Clear();

        while (_index < input.Length)
        {
            var part = ParsePart();
            _parts.Add(part);
        }

        return _parts;
    }

    private Part ParsePart()
    {
        // Indexing
        if (Match("["))
        {
            // Parse an integer
            var start = _index;
            while (_index < input.Length && char.IsDigit(input[_index]))
                _index++;

            var index = int.Parse(input.Substring(start, _index - start));
            Expect("]");
            return new(index);
        }

        if (Match("/"))
        {
            // Mapping
            var start = _index;
            while (_index < input.Length && (char.IsLetterOrDigit(input[_index]) || input[_index] is '_'))
                _index++;

            var key = input.Substring(start, _index - start);
            return new(key);
        }

        // _index is guaranteed to not be OOB here
        throw new ArgumentException($"Expected YAML path to contain the start of a mapping (/) or indexing ([), but found {input[_index]}!");
    }

    private bool Match(string expected)
    {
        if (_index + expected.Length > input.Length)
            return false;

        var start = _index;
        for (var i = 0; i < expected.Length; i++)
            if (expected[i] != input[start + i])
                return false;

        _index += expected.Length;
        return true;
    }

    private void Expect(string expected, string? error = null)
    {
        if (!Match(expected))
            throw new ArgumentException(error ?? $"Expected \"{expected}\" at index {_index}. Faulty YAML path: {input}.");
    }

    public struct Part
    {
        /// <summary>
        ///     The mapping key this part references, or null if it's not a mapping reference.
        /// </summary>
        public string? Key;
        /// <summary>
        ///     The index this part references, or -1 if it's not a sequence index reference.
        /// </summary>
        public int Index;

        public Part(string key)
        {
            Key = key;
            Index = -1;
        }

        public Part(int index)
        {
            Index = index;
            Key = null;
        }

        public bool IsMapping => Key != null;
        public bool IsIndexing => Index >= 0;
        public bool IsError => !IsMapping && !IsIndexing;

        public YamlNode? Resolve(YamlNode parent)
        {
            if (IsMapping)
            {
                if (parent is not YamlMappingNode mapping)
                    throw new ArgumentException($"Cannot resolve mapping part {this} on non-mapping node {parent}!");
                return mapping.TryGetNode(Key!, out var result) ? result : null;
            }

            if (IsIndexing)
            {
                if (parent is not YamlSequenceNode sequence)
                    throw new ArgumentException($"Cannot resolve indexing part {this} on non-sequence node {parent}!");
                return Index < sequence.Children.Count ? sequence[Index] : null;
            }
            throw new ArgumentException($"Cannot resolve part {this}!");
        }
    }
}
