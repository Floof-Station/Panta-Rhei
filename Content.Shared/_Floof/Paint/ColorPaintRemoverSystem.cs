using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Floof.Paint;

public sealed class ColorPaintRemoverSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorPaintRemoverComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ColorPaintRemoverComponent, PaintRemoverDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ColorPaintRemoverComponent, GetVerbsEvent<UtilityVerb>>(OnPaintRemoveVerb);
    }


    private void OnInteract(EntityUid uid, ColorPaintRemoverComponent component, AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { Valid: true } target
            || !HasComp<ColorPaintedComponent>(target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.CleanDelay, new PaintRemoverDoAfterEvent(), uid, args.Target, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        });
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ColorPaintRemoverComponent component, DoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Args.Target == null
            || args.Target is not { Valid: true } target
            || !TryComp(target, out ColorPaintedComponent? paint))
            return;

        paint.Enabled = false;
        _audio.PlayPredicted(component.Sound, target, args.User);
        _popup.PopupClient(Loc.GetString("paint-removed", ("target", target)), args.User, args.User, PopupType.Medium);
        _appearanceSystem.RemoveData(target, PaintVisuals.Painted);
        RemComp<ColorPaintedComponent>(target);

        args.Handled = true;
    }

    private void OnPaintRemoveVerb(EntityUid uid, ColorPaintRemoverComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var verb = new UtilityVerb()
        {
            Text = Loc.GetString("paint-remove-verb"),
            Act = () =>
            {
                _doAfter.TryStartDoAfter(
                    new DoAfterArgs(
                        EntityManager,
                        args.User,
                        component.CleanDelay,
                        new PaintRemoverDoAfterEvent(),
                        uid,
                        args.Target,
                        uid)
                    {
                        BreakOnMove = true,
                        BreakOnDamage = true,
                        MovementThreshold = 1.0f,
                    });
            },
        };

        args.Verbs.Add(verb);
    }
}
