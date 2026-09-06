using Content.Server.Actions;
using Content.Server.Body;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared._Floof.Geras;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Content.Shared.Zombies;
using Robust.Shared.Player;

namespace Content.Server._Floof.Geras;

/// <inheritdoc/>
public sealed class GerasSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly VisualBodySystem _visualBody = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // try to add geras action
        _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
    }


    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return; // i hate zomber.

        var colors = GrabHumanoidColors(uid); // begin imp
        var ent = _polymorphSystem.PolymorphEntity(uid, component.GerasPolymorphId);

        if (colors is {} colorsfr) // Match Geras to Humanoid Skin color
        {
            (var skinColor, var eyeColor) = (colorsfr.SkinColor, colorsfr.SkinColor);
            if (TryComp<RandomSpriteComponent>(ent, out var randomSprite)) // has to use random sprite
            {
                foreach (var entry in randomSprite.Selected)
                {
                    var state = randomSprite.Selected[entry.Key];
                    state.Color = entry.Key switch
                    {
                        "colorMap" => skinColor.WithAlpha(0.72f),
                        "eyesMap" => eyeColor,
                        _ => state.Color
                    };
                    randomSprite.Selected[entry.Key] = state;
                }
                Dirty(ent.Value, randomSprite);
            }
        } // end imp



        if (!ent.HasValue)
            return;

        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-others", ("entity", ent.Value)), ent.Value, Filter.PvsExcept(ent.Value), true);
        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-user"), ent.Value, ent.Value);

        args.Handled = true;
    }

    // Original from imp, rewritten for euph
    private OrganProfileData? GrabHumanoidColors(EntityUid entity)
    {
        // Nubody
        // it grabs the eye color, skin color, and sex from the character profile and shoves it into every marking
        // Neither of these 3 properties are stored anywhere else
        // I'm gonna lose my fucking shit if i spend another minute working with this code, so I'm just gonna leave this hack here
        // We try to grab the skin and eye color from the first organ that specifies it, and if everything is default, we leave the random sprites as-is
        // TODO free me from this nightmare
        if (!_visualBody.TryGatherMarkingsData(entity, null, out var profiles, out _, out _))
            return null;

        foreach (var (organCategory, data) in profiles)
        {
            if (data.SkinColor != default && data.EyeColor != default)
                return data;
        }

        return null;
    }
}
