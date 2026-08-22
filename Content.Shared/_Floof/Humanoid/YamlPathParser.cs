using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared._Floof.Humanoid;

// ReSharper disable BadExpressionBracesLineBreaks
// ReSharper disable BadEmptyBracesLineBreaks

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
