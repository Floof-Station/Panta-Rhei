using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared._Floof.Geras;
using Robust.Shared.Player;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;

namespace Content.Server._Floof.Geras;

/// <inheritdoc/>
public sealed class GerasSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

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

        if (colors != null) // Match Geras to Humanoid Skin color
        {
            (var skinColor, var eyeColor) = colors.Value;
            if (TryComp<RandomSpriteComponent>(ent, out var randomSprite)) // has to use random sprite
            {
                foreach (var entry in randomSprite.Selected)
                {
                    var state = randomSprite.Selected[entry.Key];
                    state.Color = entry.Key switch
                    {
                        "colorMap" => skinColor,
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

    private (Color, Color)? GrabHumanoidColors(EntityUid entity) //imp
    {
        if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoid)) // Get Humanoid Appearance
        {
            var skinColor = humanoid.SkinColor;
            var eyeColor = humanoid.EyeColor;
            return (skinColor, eyeColor);
        }

        return null; // if a non-humanoid or someone with no bloodstream ascends, don't modify the colors
    }
}
