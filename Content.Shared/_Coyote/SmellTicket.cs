using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This is a ticket for a pending smell.
/// </summary>
public sealed class SmellTicket(
    EntityUid sourceEntity,
    ProtoId<ScentPrototype> scentProto,
    string scentGuid,
    MapCoordinates origin,
    TimeSpan createdTime,
    float distance
)
{
    /// <summary>
    /// The proto for the scent this ticket is for
    /// </summary>
    [DataField]
    public ProtoId<ScentPrototype> ScentProto = scentProto;

    /// <summary>
    /// The unique-ish ID for the scent instance this ticket is for
    /// </summary>
    [DataField]
    public string ScentInstanceId = scentGuid;

    /// <summary>
    /// The entity that this smell came from
    /// </summary>
    [DataField]
    public EntityUid SourceEntity = sourceEntity;

    /// <summary>
    /// The prioroity of this smell ticket
    /// Higher priority tickets get processed first.
    /// Based on proximity and other factors.
    /// </summary>
    [DataField]
    public double Priority = 0.1;

    /// <summary>
    /// Map coords of where the ticket was made
    /// </summary>
    [DataField]
    public MapCoordinates OriginCoordinates = origin;

    /// <summary>
    /// Time when this ticket was created
    /// </summary>
    [DataField]
    public TimeSpan CreatedTime = createdTime;

    /// <summary>
    /// Last recorded distance from source
    /// </summary>
    [DataField]
    public float Distance = distance;

    /// <summary>
    /// Determines if this ticket's priority is higher than another ticket's priority.
    /// Used for sorting tickets.
    /// </summary>
    public int ComparePriority(SmellTicket other)
    {
        return other.Priority.CompareTo(Priority);
    }

    /// <summary>
    /// Gets the priority value for this ticket.
    /// </summary>
    public double GetPriority(IPrototypeManager prototypeManager)
    {
        if (!prototypeManager.TryIndex(ScentProto, out var scentProto))
        {
            return Priority;
        }
        return Priority * scentProto.PriorityMultiplier;
    }

    /// <summary>
    /// Sets the priority value for this ticket.
    /// </summary>
    public void SetPriority(double priority)
    {
        Priority = priority;
    }
}
