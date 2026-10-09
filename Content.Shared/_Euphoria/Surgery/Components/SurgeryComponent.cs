using Content.Shared.Body;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Euphoria.Surgery.Components;

/// <summary>
///     Handles all surgery related effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryComponent : Component
{
    [DataField]
    public List<ProtoId<SurgeryActionPrototype>> SurgeryActions = new();

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<EntityUid,Dictionary<ProtoId<SurgeryStatePrototype>,bool>>> DictOrgans = new();

    [DataField]
    public List<ProtoId<SurgeryStatePrototype>> SurgeryStates = new();

}
public sealed class SurgeryGetActionsEvent : EntityEventArgs
{
    public readonly EntityUid Person;
    public readonly SurgeryComponent Comp;

    public bool GetUnavailable;

    public List<ProtoId<SurgeryActionPrototype>> Surgeries = new();

    public SurgeryGetActionsEvent(Entity<SurgeryComponent> person, bool forced)
    {
        (Person, Comp) = person;
        GetUnavailable = forced;
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgeryFinishedEvent : DoAfterEvent
{
    [NonSerialized]
    public ProtoId<SurgeryActionPrototype> ID;

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
