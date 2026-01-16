using Content.Shared._Floof.Paint;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Floof.Paint;

public sealed class ColorPaintedVisualizerSystem : VisualizerSystem<ColorPaintedComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorPaintedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<ColorPaintedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ColorPaintedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }


    protected override void OnAppearanceChange(EntityUid uid, ColorPaintedComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !_appearance.TryGetData(uid, PaintVisuals.Painted, out bool isPainted))
            return;

        var shader = _protoMan.Index<ShaderPrototype>(component.ShaderName).Instance();
        foreach (var spriteLayer in args.Sprite.AllLayers)
        {
            if (spriteLayer is not Layer layer)
                continue;

            if (layer.Shader == null || layer.Shader == shader)
            {
                layer.Shader = shader;
                layer.Color = component.Color;
            }
        }
    }

    private void OnShutdown(EntityUid uid, ColorPaintedComponent component, ref ComponentShutdown args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;
        component.BeforeColor = sprite.Color;

        if (Terminating(uid))
            return;

        foreach (var spriteLayer in sprite.AllLayers)
        {
            if (spriteLayer is not Layer layer)
                continue;

            if (layer.ShaderPrototype == component.ShaderName)
                layer.Shader = null;
            if (layer.Color == component.Color)
                layer.Color = component.BeforeColor;
        }
    }

    private void OnHeldVisualsUpdated(EntityUid uid, ColorPaintedComponent component, HeldVisualsUpdatedEvent args) =>
        UpdateVisuals(component, args);

    private void OnEquipmentVisualsUpdated(EntityUid uid, ColorPaintedComponent component, EquipmentVisualsUpdatedEvent args) =>
        UpdateVisuals(component, args);

    private void UpdateVisuals(ColorPaintedComponent component, EntityEventArgs args)
    {
        var layers = new HashSet<string>();
        var entity = EntityUid.Invalid;

        switch (args)
        {
            case HeldVisualsUpdatedEvent hgs:
                layers = hgs.RevealedLayers;
                entity = hgs.User;
                break;
            case EquipmentVisualsUpdatedEvent eqs:
                layers = eqs.RevealedLayers;
                entity = eqs.Equipee;
                break;
        }

        if (layers.Count == 0 || !TryComp(entity, out SpriteComponent? sprite))
            return;

        foreach (var revealed in layers)
        {
            if (!sprite.LayerMapTryGet(revealed, out var layer))
                continue;

            if (!string.IsNullOrEmpty(component.ShaderName))
                sprite.LayerSetShader(layer, component.ShaderName);
            sprite.LayerSetColor(layer, component.Color);
        }
    }
}
