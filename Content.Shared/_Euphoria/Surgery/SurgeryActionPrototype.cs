using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Euphoria.Surgery;

/// <summary>
///     Actions that can be taken during surgery that can include repairing damage, changing the state of a body, or inserting/removing organs.
/// </summary>
/// <remarks>
///     This prototype does a lot of work, but is designed so that you can create
///     any kind of surgery you can imagine. It relies on certain <see cref="SurgeryStatePrototype"/> to be instantiated inside <see cref="SurgeryComponent"/>
///     otherwise the processes will fail. Do not assign SurgeryActions directly to a mob, instead use <see cref="SurgeryListPrototype"/> as a collection.
///
///     It is entirely possible to create surgeries that make no sense. In order to make a prototype that is very manipulatable, it was difficult to avoid.
/// </remarks>

[Prototype]
public sealed partial class SurgeryActionPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables]
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SurgeryActionPrototype>))]
    public string[]? Parents { get; private set; }

    [DataField]
    public LocId Description = default;

    [DataField("toolType")]
    public SurgeryToolComponent.ToolTypeEnum _type = default!;

    [DataField("icon")]
    public SpriteSpecifier? Icon = default!;

    [DataField("duration")]
    public float _duration;

    [DataField("states")]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _states = new();

    [DataField("effects")]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<ProtoId<SurgeryStatePrototype>,bool>> _effects = new();

    [DataField]
    public ProtoId<OrganCategoryPrototype>? Remove;

    [DataField]
    public ProtoId<OrganCategoryPrototype>? Insert;

    [DataField]
    public ProtoId<DamageTypePrototype>? Treatment;

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }


    public string[] getTargets()
    {
        var targets = new List<string>();

        foreach(var organ in _effects.Keys)
            targets.Add(organ.ToString());

        return targets.ToArray();
    }
}
