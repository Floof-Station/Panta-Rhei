using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.InteractionVerbs.Events;

/// <summary>
///     Raised directly on the user entity to get more interaction verbs it may allow.
///     While InteractionVerbsComponent defines which verbs may be performed on the entity,
///     This event allows to also define which verbs the entity itself may perform.<br/><br/>
///
///     Note that this is raised before IsAllowed checks are performed on any of the verbs.
/// </summary>
[ByRefEvent]
public sealed class GetInteractionVerbsEvent(EntityUid user, EntityUid target, EntityUid? used, IEnumerable<InteractionVerbIdSource> verbs)
{
    public readonly EntityUid
        User = user,
        Target = target;

    public readonly EntityUid? Used = used;

    public HashSet<InteractionVerbIdSource> Verbs = new(verbs);

    public bool Add(ProtoId<InteractionVerbPrototype> verb, InteractionVerbSource source)
    {
        if (!Verbs.Add(new(verb, source)))
            return false;

        return true;
    }
}

/// <summary>
///   Combination of an interaction verb ID and its source.
/// </summary>
public record class InteractionVerbIdSource(ProtoId<InteractionVerbPrototype> Proto, InteractionVerbSource Source)
{
    public (InteractionVerbPrototype, InteractionVerbSource) Index(IPrototypeManager protoMan)
    {
        return (protoMan.Index(Proto), Source);
    }
}
