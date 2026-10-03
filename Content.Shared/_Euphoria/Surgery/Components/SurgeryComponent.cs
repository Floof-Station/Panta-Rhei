using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    public Dictionary<string, List<EntityUid>> DictOrgans = new();


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
