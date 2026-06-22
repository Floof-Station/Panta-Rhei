using Content.Server._Floof.Examine;
using Content.Shared._Common.Consent;
using Content.Shared._Floof.Util;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Managers;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;


namespace Content.Shared._Floof.Examine;


public abstract class SharedCustomExamineSystem : EntitySystem
{
    public static ProtoId<ConsentTogglePrototype> NsfwDescConsent = "NSFWDescriptions";
    public static ProtoId<ConsentTogglePrototype> CustomExamineChangedByOthersConsent = "CustomExamineChangedByOthers";
    public static int PublicMaxLength = 256, SubtleMaxLength = 256;
    /// <summary>Max length of any content field, INCLUDING markup.</summary>
    public static int AbsolutelyMaxLength = 1024;

    /// <summary>The time it takes to update the custom examine of another entity.</summary>
    public static TimeSpan SlowCustomExamineChangeDuration = TimeSpan.FromSeconds(3);
    /// <summary>The time multiplier for changing examine of another player.</summary>
    public static float SlowCustomExaminePlayerPenalty = 2;

    private static readonly string[] AllowedTags = // This sucks, shared markup when
    [
        "bolditalic",
        "bold",
        "bullet",
        "color",
        "heading",
        "italic",
        "mono",
        "scramble", // Some people abuse it in funny ways
        "language",
    ];

    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedConsentSystem _consent = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedInteractionSystem _interactions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfters = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CustomExamineComponent, ExaminedEvent>(OnExamined);

        SubscribeAllEvent<SetCustomExamineMessage>(OnSetCustomExamineMessage);
        SubscribeLocalEvent<SetCustomExamineDoAfterEvent>(OnSetExamineDoAfter);
    }

    private void OnExamined(Entity<CustomExamineComponent> ent, ref ExaminedEvent args)
    {
        CheckExpirations(ent);
        if (ent.Comp.PublicData.Content is null && ent.Comp.SubtleData.Content is null)
            return;

        var publicData = ent.Comp.PublicData;
        var subtleData = ent.Comp.SubtleData;

        using (args.PushGroup(nameof(CustomExamineComponent), -1))
        {
            // Lots of code duplication, blegh.
            var allowNsfw = _consent.HasConsent(args.Examiner, NsfwDescConsent);
            bool hasPublic = publicData.Content is not null, hasSubtle = subtleData.Content is not null;

            bool publicConsentHidden = hasPublic && publicData.RequiresConsent && !allowNsfw,
                 subtleConsentHidden = hasSubtle && subtleData.RequiresConsent && !allowNsfw;

            // If subtle is shown, then public is guaranteed to also be shown - this is to avoid extra raycasts
            bool subtleRangeHidden = hasSubtle && !_examine.InRangeUnOccluded(args.Examiner, args.Examined, subtleData.VisibilityRange),
                 publicRangeHidden = hasPublic && (!hasSubtle || subtleRangeHidden) && !_examine.InRangeUnOccluded(args.Examiner, args.Examined, publicData.VisibilityRange);

            if (hasPublic && !publicConsentHidden && !publicRangeHidden)
                args.PushMessage(SanitizeMarkup(publicData.Content!));

            if (hasSubtle && !subtleConsentHidden && !subtleRangeHidden)
                args.PushMessage(SanitizeMarkup(subtleData.Content!));

            // If something is hidden due to consent preferences, add a note (but only if in range)
            if (hasPublic && !publicRangeHidden && publicConsentHidden || hasSubtle && !subtleRangeHidden && subtleConsentHidden)
                args.PushMarkup(Loc.GetString("custom-examine-nsfw-hidden"));
        }
    }

    private void OnSetCustomExamineMessage(SetCustomExamineMessage msg, EntitySessionEventArgs args)
    {
        var target = GetEntity(msg.Target);

        // If custom examine data is the same as previous, don't bother
        if (TryComp<CustomExamineComponent>(target, out var oldExamine)
            && oldExamine.PublicData.Equals(msg.PublicData)
            && oldExamine.SubtleData.Equals(msg.SubtleData)
        )
            return;

        if (CanInstantlyChangeExamine(args.SenderSession, target, out var reasonLoc))
        {
            SetData(msg.PublicData, msg.SubtleData, target);
            return;
        }

        if (CanSlowlyChangeExamine(args.SenderSession, target, out reasonLoc))
        {
            var user = args.SenderSession.AttachedEntity!.Value; // CanSlowlyChangeExamine ensures its not null
            if (TryStartExamineChangeDoAfter(msg.PublicData, msg.SubtleData, user, target))
                return;
        }

        // Show a popup to the user if it fails. I wanted to use a chat message here, but it feels kinda wack
        // On the other hand popups can easily be obscured by the custom examine window.
        if (_net.IsServer && reasonLoc is not null)
            _popups.PopupEntity(Loc.GetString(reasonLoc), target, args.SenderSession, PopupType.Medium);
    }

    private void OnSetExamineDoAfter(SetCustomExamineDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not {} target)
            return;

        // Sanity check
        if (!CanSlowlyChangeExamine(args.User, target, out _))
            return;

        SetData(args.PublicData, args.SubtleData, target);
        // Small popup to let other players know what happened
        _popups.PopupPredicted(Loc.GetString("custom-examine-data-changed-visibly"), target, null);
    }

    /// <summary>
    ///     Returns true if the player can instantly change custom examine.
    /// </summary>
    protected bool CanInstantlyChangeExamine(ICommonSession actor, EntityUid examinee, out string? reasonLoc)
    {
        if (actor.AttachedEntity == examinee && _actionBlocker.CanConsciouslyPerformAction(examinee)
            || _admin.IsAdmin(actor) && HasComp<GhostComponent>(actor.AttachedEntity)) // Must be an aghost, not just an adminned player
        {
            reasonLoc = null;
            return true;
        }

        reasonLoc = "custom-examine-cant-change-data-generic";
        return false;
    }

    protected bool CanSlowlyChangeExamine(ICommonSession actor, EntityUid examinee, out string? reasonLoc)
    {
        reasonLoc = null;
        return actor.AttachedEntity is { } user && CanSlowlyChangeExamine(user, examinee, out reasonLoc);
    }

    private bool CanSlowlyChangeExamine(EntityUid user, EntityUid examinee, out string? reasonLoc)
    {
        if (!_actionBlocker.CanInteract(user, examinee)
            || HasComp<GhostComponent>(user) // This sucks, but ghosts actually CAN interact with anything
            || !_interactions.InRangeAndAccessible(user, examinee))
        {
            reasonLoc = "custom-examine-cant-interact";
            return false;
        }

        // This assumes user != target, prevent the menu from showing up if the target hasn't consented to it
        if (HasComp<ActorComponent>(examinee) && !_consent.HasConsent(examinee, CustomExamineChangedByOthersConsent))
        {
            reasonLoc = "custom-examine-cant-change-data-consent";
            return false;
        }

        reasonLoc = null;
        return true;
    }

    /// <summary>
    ///     Returns true if the player can change examine at all.
    /// </summary>
    protected bool CanChangeExamine(ICommonSession actor, EntityUid examinee, out string? reasonLoc) =>
        CanInstantlyChangeExamine(actor, examinee, out reasonLoc) || CanSlowlyChangeExamine(actor, examinee, out reasonLoc);

    private void CheckExpirations(Entity<CustomExamineComponent> ent)
    {
        bool Check(ref CustomExamineData data)
        {
            if (data.Content is null
                || data.ExpireTime.Ticks <= 0
                || data.ExpireTime > _timing.CurTime)
                return false;

            data.Content = null;
            return true;
        }

        // Note: using | (bitwise or) instead of || (logical or) because the former is not short-circuiting
        if (Check(ref ent.Comp.PublicData) | Check(ref ent.Comp.SubtleData))
            Dirty(ent);
    }

    protected void TrimData(ref CustomExamineData publicData, ref CustomExamineData subtleData)
    {
        TrimData(ref publicData);
        TrimData(ref subtleData);

        if (publicData.VisibilityRange < subtleData.VisibilityRange)
            publicData.VisibilityRange = subtleData.VisibilityRange;
    }

    protected void TrimData(ref CustomExamineData data)
    {
        if (data.Content is null)
            return;

        // Shitty way to preserve and ignore markup while trimming
        var markupLength = MarkupLength(data.Content);
        if (data.Content.Length > AbsolutelyMaxLength)
            data.Content = data.Content[..AbsolutelyMaxLength];
        if (data.Content.Length - markupLength > PublicMaxLength)
            data.Content = data.Content[..(PublicMaxLength - markupLength)];

        if (data.Content.Length == 0)
            data.Content = null;
    }

    /// <summary>
    ///     Sets custom examine data on the entity and dirties it. This performs NO checks.
    /// </summary>
    private void SetData(CustomExamineData publicData, CustomExamineData subtleData, EntityUid target)
    {
        var comp = EnsureComp<CustomExamineComponent>(target);

        TrimData(ref publicData, ref subtleData);
        comp.PublicData = publicData;
        comp.SubtleData = subtleData;

        Dirty(target, comp);
    }

    /// <summary>
    ///     Tries to start a do-after that would change the custom examine of another player. Returns true if the do-after has started or has already been going.
    ///     This will perform some consent checks.
    /// </summary>
    public bool TryStartExamineChangeDoAfter(CustomExamineData publicData, CustomExamineData subtleData, EntityUid user, EntityUid target, bool quiet = false)
    {
        // Basic consent check is already done in CanChangeExamine
        // Sanitize message data - remove NSFW contents if the target didn't consent for it
        if (!_consent.HasConsent(target, NsfwDescConsent))
        {
            if (publicData.RequiresConsent)
                publicData.Content = "";
            if (subtleData.RequiresConsent)
                subtleData.Content = "";
        }

        // If it's a player, change the do-after length respectively and show a popup for them
        var delay = SlowCustomExamineChangeDuration;
        if (HasComp<ActorComponent>(target))
        {
            delay *= SlowCustomExaminePlayerPenalty;
            if (_net.IsServer && !quiet) // The target will never predict it
                _popups.PopupEntity(Loc.GetString("custom-examine-do-after-started-target", ("user", user)), target, target, PopupType.SmallCaution);
        }

        var doAfterArgs = new DoAfterArgs
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            CancelDuplicate = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            NeedHand = false,
            RequireCanInteract = true,
            Target = target,
            User = user,
            Delay = delay,
            Event = new SetCustomExamineDoAfterEvent(publicData, subtleData),
            Broadcast = true, // No component to listen on
        };

        return _doAfters.TryStartDoAfter(doAfterArgs);
    }

    protected int LengthWithoutMarkup(string text) => FormattedMessage.RemoveMarkupPermissive(text).Length;

    protected int MarkupLength(string text) => text.Length - LengthWithoutMarkup(text);

    /// <summary>
    ///     Removes disallowed tags from the formatted message.
    /// </summary>
    protected FormattedMessage SanitizeMarkup(string text)
    {
        var msg = FormattedMessage.FromMarkupPermissive(text);
        return FormattedMessageHelpers.SanitizeMarkup(msg, AllowedTags, "<bad markup>");
    }
}
