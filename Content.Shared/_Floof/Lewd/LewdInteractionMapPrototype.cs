using System.Diagnostics.CodeAnalysis;
using Content.Shared._Floof.InteractionVerbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Floof.Lewd;

/// <summary>
///     Maps all possible pairs of (user organ kind, target organ kind) to interaction verbs.
///
///     User with organs [a, b] who opens the verb menu on a target with organs [b, c] will receive all interaction verbs
///     from this map that correspond to any of the values in the cross-product between the two lists: [ab, ac, bb, bc].
///     If there's a combination of organs that this map doesn't provide an interaction verb for, then nothing will be added in that case.
/// </summary>
[Prototype]
public sealed class LewdInteractionMapPrototype : IPrototype, IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LewdInteractionMapPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true), AlwaysPushInheritance]
    public Dictionary<LewdOrganMapping, List<ProtoId<InteractionVerbPrototype>>> Map = new();
}

/// <summary>
///     RT doesn't support dictionaries with complex keys, so this struct works around that by flattening the complex key (pair of LewdOrganKind) into a string.
/// </summary>
[DataDefinition]
public partial struct LewdOrganMapping : ISelfSerialize, IEquatable<LewdOrganMapping>
{
    [DataField]
    public LewdOrganKind Item1, Item2;

    public LewdOrganMapping(LewdOrganKind item1, LewdOrganKind item2)
    {
        Item1 = item1;
        Item2 = item2;
    }

    public void Deserialize(string value)
    {
        var sep = value.IndexOf(',');
        if (sep == -1)
            return;

        Enum.TryParse(value.AsSpan(0, sep), out Item1);
        Enum.TryParse(value.AsSpan(sep + 1, value.Length - sep - 1), out Item2);
    }

    public string Serialize() => Item1.ToString() + ',' + Item2.ToString();

    public bool Equals(LewdOrganMapping other) => Item1 == other.Item1 && Item2 == other.Item2;

    public override bool Equals(object? obj) => obj is LewdOrganMapping other && Equals(other);

    public override int GetHashCode() => (int) Item1 * 127 + (int) Item2;

    public override string ToString() => Serialize();
}
