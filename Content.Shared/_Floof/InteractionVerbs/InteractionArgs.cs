using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.InteractionVerbs;

public sealed partial class InteractionArgs(
    EntityUid user,
    EntityUid target,
    EntityUid? used,
    bool canAccess,
    bool canInteract,
    bool hasHands,
    float? contestAdvantage,
    bool allowRepeat,
    InteractionVerbSource source)
{
    public EntityUid User = user,
                     Target = target;
    public EntityUid? Used = used;

    public bool CanAccess = canAccess,
                CanInteract = canInteract,
                HasHands = hasHands;

    /// <summary>
    ///     A float value between 0 and positive infinity that indicates how much stronger the user
    ///     is compared to the target in terms of contests allowed for this verb. 1.0 means no advantage or disadvantage.
    /// </summary>
    /// <remarks>Can be null, which means it's not calculated yet. That can happen before the user attempts to perform the verb.</remarks>
    public float? ContestAdvantage = contestAdvantage;

    /// <summary>
    ///     A dictionary for actions and requirements to store data between different execution stages.
    ///     For example, an action can cache some data in its CanPerform check and later use it in Perform.
    /// </summary>
    /// <remarks>
    ///     Only actions should write into this dictionary. Don't do it otherwise unless you know what you're doing.
    /// </remarks>
    public Dictionary<string, object> Blackboard => _blackboardField ??= new(3);
    private Dictionary<string, object>? _blackboardField; // null by default, allocated lazily (only if actually needed)

    /// <summary>
    ///     Override for whether the verb is allowed to repeat. If set to false, the system will not repeat the verb even if the prototype requests it.
    ///     Verb actions can write to this.
    /// </summary>
    public bool AllowRepeat = allowRepeat;

    /// <summary>
    ///     Which entity fascilated this verb. Can be checked to make a verb do different things based on where its coming from.
    /// </summary>
    public InteractionVerbSource Source = source;

    public InteractionArgs(InteractionArgs other) : this(other.User, other.Target, other.Used, other.CanAccess, other.CanInteract, other.HasHands, other.ContestAdvantage, other.AllowRepeat, other.Source) {}

    /// <summary>
    ///     Copies all relevant info from the GetVerbsEvent.
    ///     Sets unknown verb source.
    /// </summary>
    public static InteractionArgs From<T>(GetVerbsEvent<T> ev) where T : Verb =>
        new(ev.User,
            ev.Target,
            ev.Using,
            ev.CanAccess,
            ev.CanInteract,
            ev.Hands is not null,
            null,
            true,
            InteractionVerbSource.Unknown);

    /// <summary>
    ///     Tries to get a value from the blackboard as an instance of a specific type.
    /// </summary>
    public bool TryGetBlackboard<T>(string key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        if (_blackboardField == null || !_blackboardField.TryGetValue(key, out var maybeValue))
            return false;

        // Cannot use a type check here. If someone fucks up, it's gonna be on them.
        value = (T?) maybeValue;
        return value != null;
    }
}
