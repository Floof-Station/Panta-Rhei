using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared._Floof.Paint;

public abstract class SharedColorPaintSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public void Paint(EntityWhitelist? whitelist, EntityWhitelist? blacklist, EntityUid target, Color color)
    {
        if (_whitelist.IsWhitelistFail(whitelist, target)
            || _whitelist.IsWhitelistPass(blacklist, target))
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
                    || _whitelist.IsWhitelistPass(blacklist, slotEnt.Value))
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

    public void ClearPaint(EntityUid target)
    {
        if (target is not { Valid: true } || !TryComp<ColorPaintedComponent>(target, out var paint))
            return;

        paint.Enabled = false;
        _appearanceSystem.RemoveData(target, PaintVisuals.Painted);
        RemComp<ColorPaintedComponent>(target);
        Dirty(target, paint);
    }

    /// <summary>
    ///     Clamps the brightness (luminance) of a color.
    /// </summary>
    public static Color ClampBrightness(Color color, float min, float max)
    {
        var hsl = Color.ToHsl(color);
        var l = hsl.Z;
        if (l >= min && l <= max)
            return color;

        return Color.FromHsl(hsl with { Z = Math.Clamp(l, min, max) });
    }
}
