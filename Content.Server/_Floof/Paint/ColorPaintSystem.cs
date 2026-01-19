using Content.Shared._Floof.Paint;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Sprite;
using Content.Shared.SubFloor;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Floof.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public sealed class ColorPaintSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorPaintComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ColorPaintComponent, PaintDoAfterEvent>(OnPaint);
        SubscribeLocalEvent<ColorPaintComponent, GetVerbsEvent<UtilityVerb>>(OnPaintVerb);
    }


    private void OnInteract(Entity<ColorPaintComponent> entity, ref AfterInteractEvent args)
    {
        if (!args.CanReach
            || args.Target is not { Valid: true } target)
            return;

        PrepPaint(entity, target, args.User);
    }

    private void OnPaintVerb(Entity<ColorPaintComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var target = args.Target;
        var user = args.User;
        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                PrepPaint(entity, target, user);
            },
            Text = Loc.GetString("paint-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/_Floof/Interface/VerbIcons/paint.svg.192dpi.png"))
        };
        args.Verbs.Add(verb);
    }

    private void PrepPaint(Entity<ColorPaintComponent> entity, EntityUid target, EntityUid user)
    {
        // Floofstation: check if we can paint BEFORE the do-after
        if (!CanPaint(entity, target, user, out var reason))
        {
            _popup.PopupEntity(reason, user, user);
            return;
        }

        _doAfterSystem.TryStartDoAfter(
            new(EntityManager, user, entity.Comp.Delay, new PaintDoAfterEvent(), entity, target: target, used: entity)
            {
                BreakOnMove = true,
                NeedHand = true,
                BreakOnHandChange = true,
            });
    }

    private void OnPaint(Entity<ColorPaintComponent> entity, ref PaintDoAfterEvent args)
    {
        if (args.Target == null || args.Used == null || args.Handled || args.Cancelled || args.Target is not { Valid: true } target)
            return;

        // Floofstation: check if we can paint BEFORE the do-after
        if (!CanPaint(entity, target, args.User, out var reason))
        {
            _popup.PopupEntity(reason, args.User, args.User);
            return;
        }

        Paint(entity, target, args.User);
        args.Handled = true;
    }

    public void Paint(Entity<ColorPaintComponent> entity, EntityUid target, EntityUid user)
    {
        if (!CanPaint(entity, target, user, out var reason))
        {
            _popup.PopupEntity(reason, user, user);
            return;
        }

        if (!TryConsumePaint(entity))
            return;

        Paint(entity.Comp.Whitelist, entity.Comp.Blacklist, target, entity.Comp.Color);
        _audio.PlayPvs(entity.Comp.Spray, entity);
        _popup.PopupEntity(Loc.GetString("paint-success", ("target", target)), user, user, PopupType.Medium);
    }

    public void Paint(EntityWhitelist? whitelist, EntityWhitelist? blacklist, EntityUid target, Color color)
    {
        if (_whitelist.IsWhitelistFail(whitelist, target)
            || _whitelist.IsBlacklistPass(blacklist, target))
            return;

        EnsureComp<ColorPaintedComponent>(target, out var paint);
        EnsureComp<AppearanceComponent>(target);

        paint.Color = color;
        paint.Enabled = true;

        // Try to paint all clothing on the entity if it has any
        if (HasComp<InventoryComponent>(target)
            && _inventory.TryGetSlots(target, out var slotDefinitions))
            foreach (var slot in slotDefinitions)
            {
                if (!_inventory.TryGetSlotEntity(target, slot.Name, out var slotEnt)
                    || _whitelist.IsWhitelistFail(whitelist, slotEnt.Value)
                    || _whitelist.IsBlacklistPass(blacklist, slotEnt.Value))
                    continue;

                EnsureComp<ColorPaintedComponent>(slotEnt.Value, out var slotToPaint);
                EnsureComp<AppearanceComponent>(slotEnt.Value);
                slotToPaint.Color = color;
                _appearanceSystem.SetData(slotEnt.Value, PaintVisuals.Painted, true);
                Dirty(slotEnt.Value, slotToPaint);
            }

        _appearanceSystem.SetData(target, PaintVisuals.Painted, true);
        Dirty(target, paint);
    }

    /// <summary>
    /// Checks if an entity can be painted. Returns false and provides the reason if it cannot be.
    /// </summary>
    public bool CanPaint(Entity<ColorPaintComponent> paint, EntityUid target, EntityUid user, out string? reason)
    {
        if (_openable.IsClosed(paint))
        {
            reason = Loc.GetString("paint-closed", ("used", paint));
            return false;
        }

        if (!_solutionContainer.TryGetSolution(paint.Owner, paint.Comp.Solution, out _, out var solution) || solution.Volume <= 0)
        {
            reason = Loc.GetString("paint-empty", ("used", paint));
            return false;
        }

        if (HasComp<ColorPaintedComponent>(target) || HasComp<RandomSpriteComponent>(target))
        {
            reason = Loc.GetString("paint-failure-painted", ("target", target));
            return false;
        }

        if (_whitelist.IsWhitelistFail(paint.Comp.Whitelist, target)
            || _whitelist.IsBlacklistPass(paint.Comp.Blacklist, target))
        {
            reason = Loc.GetString("paint-failure", ("target", target));
            return false;
        }

        reason = null;
        return true;
    }

    // Floofstation note: pre-rebase this method used to be called CanPaint. Painting something would consume reagents TWICE.
    private bool TryConsumePaint(Entity<ColorPaintComponent> reagent)
    {
        if (!_solutionContainer.TryGetSolution(reagent.Owner, reagent.Comp.Solution, out _, out var solution))
            return false;

        var quantity = solution.RemoveReagent(reagent.Comp.Reagent, reagent.Comp.ConsumptionUnit);
        return (quantity > 0);
    }
}
