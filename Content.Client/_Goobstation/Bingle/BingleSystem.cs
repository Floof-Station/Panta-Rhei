using Robust.Client.GameObjects;
using Content.Shared._Goobstation.Bingle;
using Content.Shared.CombatMode;

namespace Content.Client._Goobstation.Bingle;

/// <summary>
///   handles the upgrade apperance from normal bingle to upgraded bingle
/// </summary>
public sealed class BingleSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BingleComponent, ToggleCombatActionEvent>(OnCombatToggle);
        SubscribeLocalEvent<BingleComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    //make eyse glow red when combat mode engaged.
    private void OnCombatToggle(EntityUid uid, BingleComponent component, ToggleCombatActionEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        _appearance.OnChangeData(uid, sprite);
    }

    public void OnAppearanceChange(EntityUid uid, BingleComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;
        if (!TryComp<CombatModeComponent>(uid, out var combat))
            return;
        if (!sprite.LayerMapTryGet(BingleVisual.Combat, out var layer))
            return;
        sprite.LayerSetVisible(layer, combat.IsInCombatMode);
    }
}
