using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Floof.InteractionVerbs.Components;
using Content.Shared._Floof.InteractionVerbs.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Contests;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared._Floof.InteractionVerbs.InteractionPopupPrototype.Prefix;
using static Content.Shared._Floof.InteractionVerbs.InteractionVerbPrototype.ContestType;
using static Content.Shared._Floof.InteractionVerbs.InteractionVerbPrototype.EffectTargetSpecifier;

namespace Content.Shared._Floof.InteractionVerbs;

public abstract partial class SharedInteractionVerbsSystem : EntitySystem
{
    public static readonly VerbCategory InteractionCategory = new("verb-categories-interaction", null);

    private readonly InteractionAction.VerbDependencies _verbDependencies = new();
    private List<InteractionVerbPrototype> _globalPrototypes = default!;
    private List<(InteractionVerbPrototype, InteractionVerbSource)> _globalPrototypesWithSource = default!;

    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfters = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IDependencyCollection _deps = default!;

    public override void Initialize()
    {
        _deps.InjectDependencies(_verbDependencies);

        LoadGlobalVerbs();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<Components.InteractionVerbsComponent, GetVerbsEvent<InteractionVerb>>(OnGetOthersVerbs);
        SubscribeLocalEvent<Components.OwnInteractionVerbsComponent, GetVerbsEvent<InnateVerb>>(OnGetOwnVerbs);
        SubscribeLocalEvent<HandsComponent, GetInteractionVerbsEvent>(OnHandsInteractionVerbs);
        SubscribeLocalEvent<InteractionVerbDoAfterEvent>(OnDoAfterFinished);
    }

    private void LoadGlobalVerbs()
    {
        _globalPrototypes = _protoMan.EnumeratePrototypes<InteractionVerbPrototype>()
            .Where(v => v is { Global: true, Abstract: false })
            .ToList();
        _globalPrototypesWithSource = _globalPrototypes
            .Select(it => (it, InteractionVerbSource.Global))
            .ToList();
    }

    #region event handling

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<InteractionVerbPrototype>())
            return;

        LoadGlobalVerbs();
    }

    private void OnGetOthersVerbs(Entity<Components.InteractionVerbsComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        // TODO: everything was moved under GetVerbsEvent<InnateVerb> because otherwise verbs will be grouped by verb type which is gonna be confusing to players
        // Dunrab suggested allowing the verb prototype to choose which verb type to use, but i dont see a point since they are all effectively the same.
    }

    private void OnGetOwnVerbs(Entity<Components.OwnInteractionVerbsComponent> entity, ref GetVerbsEvent<InnateVerb> args)
    {
        // GetVerbsEvent<InnateVerb> is only ever raised on the user. Rework this system if that ever changes.
        // It primarily depends on the fact that raising an event on [entity] is equivalent to raising it on [args.User] and so on
        DebugTools.Assert(entity.Owner == args.User);

        // List all innate verbs of the user
        var allVerbs = entity.Comp.AllowedVerbs.Select(it =>
            new InteractionVerbIdSource(it, InteractionVerbSource.UserVerbs));

        // Add all interaction verbs from the user via an event
        var getVerbsEv = new GetInteractionVerbsEvent(args.User, args.Target, args.Using, allVerbs);
        RaiseLocalEvent(entity, ref getVerbsEv, true);

        // Index and add global ones
        var allVerbsIndexed =
            getVerbsEv.Verbs.Select(it => it.Index(_protoMan)).ToList();

        allVerbsIndexed.AddRange(_globalPrototypesWithSource);

        // Add to GetVerbsEvent
        AddAll(allVerbsIndexed, args, () => new InnateVerb());
    }

    private void OnHandsInteractionVerbs(Entity<HandsComponent> ent, ref GetInteractionVerbsEvent args)
    {
        if (args.Used is not { } used || !TryComp<ToolInteractionVerbsComponent>(used, out var toolVerbsComp))
            return;

        foreach (var verb in toolVerbsComp.AllowedVerbs)
            args.Add(verb, InteractionVerbSource.ToolVerbs);
    }

    private void OnDoAfterFinished(InteractionVerbDoAfterEvent ev)
    {
        if (ev.Cancelled || ev.Handled || !_protoMan.TryIndex(ev.VerbPrototype, out var proto))
            return;

        PerformVerb(proto, ev.VerbArgs!, out var success);
        ev.Handled = true;

        if (success && proto.Repeat && ev.VerbArgs?.AllowRepeat == true)
            ev.Repeat = true;
    }

    #endregion

    #region public api

    /// <summary>
    ///     Starts the verb, checking if it can be performed first, unless forced.
    ///     Upon success, this method will either start a do-after, or pass control to <see cref="PerformVerb"/>.
    /// </summary>
    // TODO this function is an active battlefield
    public bool StartVerb(InteractionVerbPrototype proto, InteractionArgs args, bool force = false)
    {
        if (!TryComp<Components.OwnInteractionVerbsComponent>(args.User, out var ownInteractions)
            || !force && !CheckVerbCooldown(proto, args, out _, ownInteractions))
            return false;

        // If contest advantage wasn't calculated yet, calculate it now and ensure it's in the allowed range
        var contestAdvantageValid = true;
        if (args.ContestAdvantage is null)
            CalculateAdvantage(proto, ref args, out contestAdvantageValid);

        if (!_net.IsClient
            && !force
            && (!contestAdvantageValid || proto.Action?.CanPerform(args, proto, true, _verbDependencies) != true))
        {
            CreateVerbEffects(proto.EffectFailure, Fail, proto, args);
            return false;
        }

        var attemptEv = new InteractionVerbAttemptEvent(proto, args);
        RaiseLocalEvent(args.User, ref attemptEv);
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            CreateVerbEffects(proto.EffectFailure, Fail, proto, args);
            return false;
        }
        if (attemptEv.Handled)
            return true;

        var cooldown = proto.Cooldown;
        var delay = proto.Delay;
        if (args.User == args.Target)
            delay *= proto.SelfInteractDelayFactor;
        if (proto.ContestDelay)
            delay /= args.ContestAdvantage!.Value;
        if (proto.ContestCooldown)
            cooldown /= args.ContestAdvantage!.Value;

        StartVerbCooldown(proto, args, cooldown, ownInteractions);

        // Delay can become zero if the contest advantage is infinity or just really large...
        if (delay <= TimeSpan.Zero)
        {
            PerformVerb(proto, args, out _);
            return true;
        }

        var doAfter = new DoAfterArgs(proto.DoAfter)
        {
            User = args.User,
            Target = args.Target,
            EventTarget = EntityUid.Invalid, // Raised broadcast
            Broadcast = true,
            BreakOnHandChange = proto.RequiresHands,
            NeedHand = proto.RequiresHands,
            RequireCanInteract = proto.RequiresCanInteract, // Floof - actual CanInteract
            Delay = delay,
            Event = new InteractionVerbDoAfterEvent(proto.ID, args),
        };

        var isSuccess = _doAfters.TryStartDoAfter(doAfter);
        if (isSuccess)
            CreateVerbEffects(proto.EffectDelayed, Delayed, proto, args);

        return isSuccess;
    }

    /// <summary>
    ///     Performs an additional CanPerform check (unless forced) and then actually performs the action of the verb
    ///     and shows a success/failure popup.
    /// </summary>
    /// <remarks>This does nothing on client, as the client has no clue about verb actions. Only the server should ever perform verbs.</remarks>
    public void PerformVerb(InteractionVerbPrototype proto, InteractionArgs args, out bool success, bool force = false)
    {
        success = true;
        if (_net.IsClient)
            return; // this leads to issues

        if (!PerformChecks(proto, ref args, out _, out _) && !force
            || !proto.Action!.CanPerform(args, proto, false, _verbDependencies) && !force
            || !proto.Action.Perform(args, proto, _verbDependencies))
        {
            success = false;
            CreateVerbEffects(proto.EffectFailure, Fail, proto, args);
            return;
        }

        // Success
        CreateVerbEffects(proto.EffectSuccess, Success, proto, args);
    }

    #endregion

    #region private api

    /// <summary>
    ///     Creates verbs for all listed prototypes that match their own requirements. Uses the provided factory to create new verb instances.
    /// </summary>
    // Note: using `where T : Verb, new()` here results in a sandbox violation... Yea we peasants don't get OOP in ss14.
    private void AddAll<T>(IEnumerable<(InteractionVerbPrototype, InteractionVerbSource)> verbs, GetVerbsEvent<T> args, Func<T> factory) where T : Verb
    {
        // Don't add verbs to ghosts. Ghost system will also cancel all verbs by/on non-admin ghosts.
        if (TryComp<GhostComponent>(args.User, out var ghost) && !ghost.CanGhostInteract)
            return;

        var ownInteractions = EnsureComp<Components.OwnInteractionVerbsComponent>(args.User);
        foreach (var (proto, source) in verbs)
        {
            DebugTools.AssertNotEqual(proto.Abstract, true, "Attempted to add a verb with an abstract prototype.");
            DebugTools.AssertNotEqual(source, InteractionVerbSource.Unknown, "Verb source evaluated to unknown?");

            // If the prototype doesn't allow coming from this source, skip it outright
            // We do it here instead of inside PerformChecks to skip all the overhead. Verb source won't change down the line so it doesn't matter.
            if (!proto.AllowedSource.HasFlag(source))
                continue;

            var name = proto.Name;
            if (args.Verbs.Any(v => v.Text == name))
                continue;

            var verbArgs = InteractionArgs.From(args);
            verbArgs.Source = source;
            var isEnabled = PerformChecks(proto, ref verbArgs, out var skipAdding, out var errorLocale);

            if (skipAdding)
                continue;

            var verb = factory.Invoke();
            CopyVerbData(proto, verb);
            verb.Act = () => StartVerb(proto, verbArgs);
            verb.Disabled = !isEnabled;

            if (!isEnabled)
                verb.Message = Loc.GetString(errorLocale!);

            if (isEnabled && !CheckVerbCooldown(proto, verbArgs, out var remainingTime, ownInteractions))
            {
                verb.Disabled = true;
                verb.Message = Loc.GetString("interaction-verb-cooldown", ("seconds", remainingTime.TotalSeconds));
            }

            args.Verbs.Add(verb);
        }
    }

    /// <summary>
    ///     Performs all requirement/action checks on the verb. Returns true if the verb can be executed right now.
    ///     The skipAdding output param indicates whether the caller should skip adding this verb to the verb list, if applicable.
    /// </summary>
    private bool PerformChecks(InteractionVerbPrototype proto, ref InteractionArgs args, out bool skipAdding, [NotNullWhen(false)] out string? errorLocale)
    {
        if (!proto.AllowSelfInteract && args.User == args.Target
            || !Transform(args.User).Coordinates.TryDistance(EntityManager, Transform(args.Target).Coordinates, out var distance))
        {
            skipAdding = true;
            errorLocale = "interaction-verb-invalid-target";
            return false;
        }

        if (proto.Requirement?.IsMet(args, proto, _verbDependencies) == false)
        {
            skipAdding = proto.HideByRequirement;
            errorLocale = "interaction-verb-invalid";
            return false;
        }

        // TODO: we skip this check since the client is not aware of actions. This should be changed, maybe make actions mixed server/client?
        if (proto.Action?.IsAllowed(args, proto, _verbDependencies) != true && !_net.IsClient)
        {
            skipAdding = proto.HideWhenInvalid;
            errorLocale = "interaction-verb-invalid";
            return false;
        }

        skipAdding = false;
        if (proto.RequiresHands && !args.HasHands)
        {
            errorLocale = "interaction-verb-no-hands";
            return false;
        }

        // Floofstation - added this block
        if (proto.RequiresConsciousness && !_actionBlocker.CanConsciouslyPerformAction(args.User))
        {
            errorLocale = "interaction-verb-unconscious";
            return false;
        }

        // Floof - added RequiresCanInteract
        if (proto.RequiresCanInteract && !args.CanInteract || proto.RequiresCanAccess && !args.CanAccess || !proto.Range.IsInRange(distance))
        {
            errorLocale = "interaction-verb-cannot-reach";
            return false;
        }

        // Calculate contest advantage early if required
        if (proto.ContestAdvantageRange is not null)
        {
            CalculateAdvantage(proto, ref args, out var canPerform);

            if (!canPerform)
            {
                errorLocale = "interaction-verb-too-" + (args.ContestAdvantage > 1f ? "strong" : "weak");
                return false;
            }
        }

        errorLocale = null;
        return true;
    }

    /// <summary>
    ///     Calculates the effective contest advantage for the verb and writes their clamped value to <see cref="InteractionArgs.ContestAdvantage"/>.
    /// </summary>
    private void CalculateAdvantage(InteractionVerbPrototype proto, ref InteractionArgs args, out bool canPerform)
    {
        args.ContestAdvantage = 1f;
        canPerform = true;

        var contests = proto.AllowedContests;
        if (contests == None)
            return;

        // We don't use EveryContest here because it's straight up bad
        if (contests.HasFlag(Mass))
            args.ContestAdvantage *= _contests.MassContest(args.User, args.Target, true, 10f);
        if (contests.HasFlag(Stamina))
            args.ContestAdvantage *= _contests.MassContest(args.User, args.Target, true, 10f);
        if (contests.HasFlag(Health))
            args.ContestAdvantage *= _contests.MassContest(args.User, args.Target, true, 10f);

        canPerform = proto.ContestAdvantageRange?.IsInRange(args.ContestAdvantage.Value) ?? true;
        args.ContestAdvantage = proto.ContestAdvantageLimit.Clamp(args.ContestAdvantage.Value);
    }

    private void CopyVerbData(InteractionVerbPrototype proto, Verb verb)
    {
        VerbCategory category = InteractionCategory;
        if (proto.Category is not null && _protoMan.Resolve(proto.Category, out var catProto))
            category = catProto.Materialize();

        verb.Text = proto.Name;
        verb.Message = proto.Description;
        verb.DoContactInteraction = proto.DoContactInteraction;
        verb.Priority = proto.Priority;
        verb.Icon = proto.Icon;
        verb.Category = category;
    }

    /// <summary>
    ///     Checks if the verb is on cooldown. Returns true if the verb can be used right now.
    /// </summary>
    private bool CheckVerbCooldown(InteractionVerbPrototype proto, InteractionArgs args, out TimeSpan remainingTime, Components.OwnInteractionVerbsComponent? comp = null)
    {
        remainingTime = TimeSpan.Zero;
        if (!Resolve(args.User, ref comp))
            return false;

        var cooldownTarget = proto.GlobalCooldown ? EntityUid.Invalid : args.Target;
        if (!comp.Cooldowns.TryGetValue((proto.ID, cooldownTarget), out var cooldown))
            return true;

        remainingTime = cooldown - _timing.CurTime;
        return remainingTime <= TimeSpan.Zero;
    }

    private void StartVerbCooldown(InteractionVerbPrototype proto, InteractionArgs args, TimeSpan cooldown, Components.OwnInteractionVerbsComponent? comp = null)
    {
        if (!Resolve(args.User, ref comp))
            return;

        var cooldownTarget = proto.GlobalCooldown ? EntityUid.Invalid : args.Target;
        comp.Cooldowns[(proto.ID, cooldownTarget)] = _timing.CurTime + cooldown;

        // We also clean up old cooldowns here to avoid a memory leak... This is probably a bad place to do it.
        // TODO might wanna switch to a list because dict is probably overkill for this task given we clean it up often.
        foreach (var (key, time) in comp.Cooldowns.ToArray())
        {
            if (time < _timing.CurTime)
                comp.Cooldowns.Remove(key);
        }
    }

    private void CreateVerbEffects(InteractionVerbPrototype.EffectSpecifier? specifier, InteractionPopupPrototype.Prefix prefix, InteractionVerbPrototype proto, InteractionArgs args)
    {
        // Not doing effects on client because it causes issues
        if (specifier is null || _net.IsClient)
            return;

        var (user, target, used) = (args.User, args.Target, args.Used);

        // Effect targets for different players
        var userTarget = specifier.EffectTarget is User or UserThenTarget or TargetThenUser ? user : target;
        var targetTarget = specifier.EffectTarget is Target or UserThenTarget or TargetThenUser ? target : user;
        var othersTarget = specifier.EffectTarget is Target or UserThenTarget ? target : user;
        var othersFilter = Filter.Pvs(othersTarget).RemoveWhereAttachedEntity(ent => ent == user || ent == target);

        // Popups
        if (_protoMan.TryIndex(specifier.Popup, out var popup))
        {
            var locPrefix = $"interaction-{proto.ID}-{prefix.ToString().ToLower()}";

            (string, object)[] localeArgs =
            [
                ("user", Identity.Entity(user, EntityManager)), // Floof - use identity
                ("target", Identity.Entity(target, EntityManager)), // Floof - use identity
                ("used", used != null ? Identity.Entity(used.Value, EntityManager) : EntityUid.Invalid),
                ("selfTarget", user == target),
                ("hasUsed", used != null)
            ];

            // User popup
            var userSuffix = popup.SelfSuffix ?? popup.OthersSuffix;
            if (userSuffix is not null)
                PopupEffects(Loc.GetString($"{locPrefix}-{userSuffix}-popup", localeArgs), userTarget, Filter.Entities(user), false, popup);

            // Target popup
            var targetSuffix = popup.TargetSuffix ?? popup.OthersSuffix;
            if (targetSuffix is not null && user != target)
                PopupEffects(Loc.GetString($"{locPrefix}-{targetSuffix}-popup", localeArgs), targetTarget, Filter.Entities(target), false, popup);

            // Others popup
            var othersSuffix = popup.OthersSuffix;
            if (othersSuffix is not null)
                PopupEffects(Loc.GetString($"{locPrefix}-{othersSuffix}-popup", localeArgs), othersTarget, othersFilter, true, popup, clip: true);
        }

        // Sounds
        if (specifier.Sound is { } sound)
        {
            // TODO we have a choice between having an accurate sound source or saving on an entity spawn...
            _audio.PlayEntity(sound, Filter.Entities(user, target), target, false, specifier.SoundParams ?? sound.Params); // Floof - use sound params if no custom ones are provided

            if (specifier.SoundPerceivedByOthers)
                _audio.PlayEntity(sound, othersFilter, othersTarget, false, specifier.SoundParams ?? sound.Params); // Floof - use sound params if no custom ones are provided
        }
    }

    private void PopupEffects(string message, EntityUid target, Filter filter, bool recordReplay, InteractionPopupPrototype popup, bool clip = false)
    {
        // Sending a chat message will result in a popup anyway
        // TODO this needs to be fixed probably. Popups and chat messages should be independent.
        if (popup.LogPopup)
            SendChatLog(message, target, filter, popup, clip);
        else
            _popups.PopupEntity(message, target, filter, recordReplay, popup.PopupType);
    }

    protected virtual void SendChatLog(string message, EntityUid source, Filter filter, InteractionPopupPrototype popup, bool clip) { }

    #endregion
}
