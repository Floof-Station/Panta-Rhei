using System.Linq;
using Content.Shared._Floof.Clothing.SlotBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared._Floof.Clothing.SlotBlocker.SlotBlockerSystem.CheckType;

namespace Content.Shared._Floof.Humanoid.ModifyUndies;

/// <summary>
/// This is a component that lets you show/hide specific underwear slots.
///
/// </summary>
public sealed class ModifyUndiesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SlotBlockerSystem _slotBlocker = default!;
    [Dependency] private readonly INetManager _net = default!;

    public static readonly VerbCategory UndiesCat =
        new("verb-categories-undies", "/Textures/_Floof/Interface/VerbIcons/undies.png");

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModifyUndiesComponent, GetVerbsEvent<Verb>>(AddModifyUndiesVerb);
        SubscribeLocalEvent<ModifyUndiesComponent, ModifyUndiesDoAfterEvent>(ToggleUndies);
    }

    private void AddModifyUndiesVerb(EntityUid uid, ModifyUndiesComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;
        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var humApp))
            return;

        if (args.User != args.Target
            && _slotBlocker.IsSlotObstructedOrOccupied(args.Target, null, IgnoreBlockerPreference, SlotFlags.INNERCLOTHING, out _))
            return; // mainly so people cant just spy on others undies *too* easily

        var isMine = args.User == args.Target;

        // okay go through their markings, and find all the undershirts and underwear markings
        // <marking_ID>, list:(localized name, bodypart enum, isvisible)
        foreach (var marking in humApp.MarkingSet.Markings.Values.SelectMany(markingLust => markingLust))
        {
            if (!_markingManager.TryGetMarking(marking, out var mProt))
                continue;

            // check if the Bodypart is in the component's BodyPartTargets
            if (!component.BodyPartTargets.Contains(mProt.BodyPart))
                continue;

            var localizedName = Loc.GetString($"marking-{mProt.ID}");
            var partSlot = mProt.BodyPart;
            var isVisible = !humApp.HiddenMarkings.Contains(mProt.ID);
            if (mProt.Sprites.Count < 1)
                continue; // no sprites means its not visible means its kinda already off and you cant put it on

            var verbIcon = partSlot switch
            {
                HumanoidVisualLayers.UndergarmentTop => new(new("/Textures/_Floof/Interface/VerbIcons/bra.png")),
                HumanoidVisualLayers.UndergarmentBottom => new(new("/Textures/_Floof/Interface/VerbIcons/underpants.png")),
                _ => new SpriteSpecifier.Texture(new("/Textures/_Floof/Interface/VerbIcons/undies.png"))
            };

            Verb verb = new()
            {
                Text = Loc.GetString(
                    "modify-undies-verb-text",
                    ("undies", localizedName),
                    ("isVisible", isVisible),
                    ("isMine", isMine),
                    ("target", Identity.Entity(args.Target, EntityManager))
                ),
                Icon = verbIcon,
                Category = UndiesCat,
                Act = () => OnModifyUndiesVerb(args, marking, localizedName, isVisible),
                Disabled = false,
                Message = null
            };
            args.Verbs.Add(verb);
            continue;

            // add the verb
        }
    }

    private void OnModifyUndiesVerb(GetVerbsEvent<Verb> args, Marking marking, string localizedName, bool isVisible)
    {
        if (_net.IsClient)
            return; // Don't predict

        var ev = new ModifyUndiesDoAfterEvent(marking, localizedName, isVisible);
        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            2f,
            ev,
            args.Target,
            args.Target,
            used: args.User) { Hidden = false, MovementThreshold = 0.2f, RequireCanInteract = true, BlockDuplicate = true };

        string gString;
        if (args.User == args.Target)
        {
            gString = isVisible ? "undies-removed-self-start" : "undies-equipped-self-start";
            _popupSystem.PopupCoordinates(Loc.GetString(gString, ("undie", localizedName)),
                Transform(args.Target).Coordinates,
                Filter.Entities(args.Target),
                true,
                PopupType.Medium);
        }
        // someone doing this to someone else
        else
        {
            // to the user
            gString = isVisible ? "undies-removed-user-start" : "undies-equipped-user-start";
            _popupSystem.PopupCoordinates(Loc.GetString(gString, ("undie", localizedName)),
                Transform(args.Target).Coordinates,
                Filter.Entities(args.User),
                true,
                PopupType.Medium);
            // to the target
            gString = isVisible
                ? "undies-removed-target-start"
                : "undies-equipped-target-start";
            _popupSystem.PopupCoordinates(Loc.GetString(gString, ("undie", localizedName), ("user", Identity.Entity(args.User, EntityManager))),
                Transform(args.Target).Coordinates,
                Filter.Entities(args.Target),
                true,
                PopupType.MediumCaution);
        }

        // and then play a sound!
        var rufthleAudio = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");
        _audio.PlayEntity(rufthleAudio,
            Filter.Entities(args.User, args.Target),
            args.Target,
            false,
            AudioParams.Default.WithVariation(0.8f).WithVolume(0.3f));
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        // ToggleUndies(uid, mProt, isVisible, localizedName, args.User, args.Target, humApp);
    }

    private void ToggleUndies(EntityUid uid, ModifyUndiesComponent component, ModifyUndiesDoAfterEvent args)
    {
        if (_net.IsClient)
            return; // Don't predict

        if (!_markingManager.TryGetMarking(args.Marking, out var mProt))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var humApp))
            return;

        SetMarkingVisibility(
            (uid, humApp),
            mProt.ID,
            !args.IsVisible
        );
        // then make a text bubble!
        // one for the doer, one for the target
        // and one if the doer is the target
        // Effect targets for different players
        // Popups
        string gString;
        if (args.User == args.Target.Value)
        {
            gString = args.IsVisible ? "undies-removed-self" : "undies-equipped-self";
            _popupSystem.PopupCoordinates(
                Loc.GetString(gString, ("undie", args.MarkingPrototypeName)),
                Transform(args.Target.Value).Coordinates,
                Filter.Entities(args.Target.Value),
                true,
                PopupType.Medium);
        }
        // someone doing this to someone else
        else
        {
            // to the user
            gString = args.IsVisible
                ? "undies-removed-user"
                : "undies-equipped-user";
            _popupSystem.PopupCoordinates(
                Loc.GetString(
                    gString,
                    ("undie", args.MarkingPrototypeName)
                ),
                Transform(args.Target.Value).Coordinates,
                Filter.Entities(args.User),
                true,
                PopupType.Medium);
            // to the target
            gString = args.IsVisible
                ? "undies-removed-target"
                : "undies-equipped-target";
            _popupSystem.PopupCoordinates(
                Loc.GetString(
                    gString,
                    ("undie", args.MarkingPrototypeName),
                    ("user", Identity.Entity(args.User, EntityManager))
                ),
                Transform(args.Target.Value).Coordinates,
                Filter.Entities(args.Target.Value),
                true,
                PopupType.Medium);
        }

        // and then play a sound!
        var rufthleAudio = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");
        _audio.PlayEntity(
            rufthleAudio,
            Filter.Entities(args.User, args.Target.Value),
            args.Target.Value,
            false,
            AudioParams.Default.WithVariation(0.8f).WithVolume(0.5f));
    }

    public void SetMarkingVisibility(
        Entity<HumanoidAppearanceComponent?> ent,
        string markingId,
        bool visible)
    {
        if (!_markingManager.Markings.TryGetValue(markingId, out var prototype))
            return;
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (visible)
            ent.Comp.HiddenMarkings.Remove(markingId);
        else
            ent.Comp.HiddenMarkings.Add(markingId);

        Dirty(ent);
    }

    /// <summary>
    ///     The above is a nightmare. This method checks if the relevant underwear slot is "pulled down" by checking if any marking on that layer is hidden.
    /// </summary>
    public bool IsMissingUndergarment(Entity<HumanoidAppearanceComponent?> ent, HumanoidVisualLayers layer)
    {
        // TODO THIS IS TECHNICAL DEBT
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var hiddenMarkingId in ent.Comp.HiddenMarkings)
        {
            if (_protoMan.TryIndex<MarkingPrototype>(hiddenMarkingId, out var hiddenMarking) &&
                hiddenMarking.BodyPart == layer)
                return true;
        }

        // We need to check if the mob even has those markings in the first place
        var markingCat = MarkingCategoriesConversion.FromHumanoidVisualLayers(layer);
        var markings = ent.Comp.MarkingSet.Markings;

        return !markings.ContainsKey(markingCat) || markings[markingCat].Count == 0;
    }
}
