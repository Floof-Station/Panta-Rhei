using System.Linq;
using System.Numerics;
using Content.Shared._Floof.Paint;
using Content.Shared._Floof.Ropes.Components;
using Content.Shared._Floof.Util;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Floof.Ropes;

public sealed class RopeVisualsOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly IEntityManager _entMan;
    private readonly IGameTiming _timing;
    private readonly SpriteSystem _sprites;
    private readonly SharedTransformSystem _xform;
    private readonly IPrototypeManager _prototypeManager;

    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<ColorPaintedComponent> _paintQuery;

    private ISawmill Log => Logger.GetSawmill("rope-visuals");
    private Ticker _logTicker = new(TimeSpan.FromSeconds(3));

    public RopeVisualsOverlay(IEntityManager entMan)
    {
        ZIndex = (int) Shared.DrawDepth.DrawDepth.BelowMobs;

        _entMan = entMan;
        _timing = IoCManager.Resolve<IGameTiming>();
        _sprites = _entMan.System<SpriteSystem>();
        _xform = _entMan.System<SharedTransformSystem>();
        _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        IoCManager.Resolve<IEyeManager>();

        _xformQuery = _entMan.GetEntityQuery<TransformComponent>();
        _spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();
        _paintQuery = _entMan.GetEntityQuery<ColorPaintedComponent>();
        _entMan.GetEntityQuery<RopeComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        worldHandle.SetTransform(Vector2.Zero, Angle.Zero);

        var query = _entMan.EntityQueryEnumerator<RopeComponent>();
        while (query.MoveNext(out var ropeUid, out var ropeComp))
        {
            if (ropeComp.IsDisabled)
                continue;

            // Get configuration and sprite
            if (!_prototypeManager.TryIndex(ropeComp.Configuration, out var config))
            {
                if (_logTicker.TryUpdate(_timing))
                    Log.Warning($"Rope {ropeUid} has invalid configuration prototype {ropeComp.Configuration}");
                continue;
            }

            var texture = _sprites.Frame0(config.Sprite);

            // Collect link positions (as map coords) and validate map id
            var linkCount = ropeComp.Links.Count;
            var positions = new Vector2[linkCount + 2];
            var canRender = true;

            for (var i = 0; i < linkCount; i++)
            {
                var linkEntity = ropeComp.Links[i].LinkEntity;
                if (!_xformQuery.TryGetComponent(linkEntity, out var xform) || xform.MapID != args.MapId)
                {
                    canRender = false;
                    break;
                }

                // Note: this sets array indices 1..(linkCount+1) while the array has 0..(linkCount+2)
                positions[i + 1] = _xform.ToMapCoordinates(xform.Coordinates, false).Position;
            }

            // Set first and last array elements to rope anchors
            if (ropeComp.ConnectedStart is {} start)
                positions[0] = GetAnchorPosition(start.Anchor, args);

            if (ropeComp.ConnectedEnd is {} end)
                positions[^1] = GetAnchorPosition(end.Anchor, args);

            if (!canRender)
                continue;

            // Depending on whether or not rope ends are connected, some array elements may need to be skipped
            var startIdx = ropeComp.ConnectedStart.HasValue ? 0 : 1;
            var endIdx = positions.Length - (ropeComp.ConnectedEnd.HasValue ? 1 : 2);
            var segmentCount = endIdx - startIdx;

            if (segmentCount < 1 || segmentCount > 64)
            {
                if (_logTicker.TryUpdate(_timing))
                    Log.Warning($"Rope {ropeUid} has an unsupported number of segments ({segmentCount}), skipping.");
                continue;
            }

            var color = ropeComp.Color ?? Color.White;
            DrawPolyline(positions, startIdx, endIdx, worldHandle, texture, color);
        }
    }

    /// <summary>
    ///     Draws a polyline in world coordinates between the vertices. Segments are drawn using the provided texture.
    ///     Texture is tiled to make transitions look mostly seamless.
    /// </summary>
    private void DrawPolyline(
        Vector2[] vertices,
        int startIdx,
        int endIdx,
        DrawingHandleWorld worldHandle,
        Texture texture,
        Color color)
    {
        if (endIdx - startIdx < 1)
            return;

        var textureWidth = texture.Width / (float)EyeManager.PixelsPerMeter;
        var textureHeight = texture.Height / (float)EyeManager.PixelsPerMeter;

        // Offset within the current texture tile (0 <= texOffset < textureHeight)
        var texOffset = 0f;

        // Go through each segment and draw it as one or more texture tiles
        for (var i = startIdx; i < endIdx; i++)
        {
            var segmentStart = vertices[i];
            var segmentEnd = vertices[i + 1];
            var segmentVec = segmentEnd - segmentStart;
            var segmentLength = segmentVec.Length();
            if (segmentLength < 0.001f)
                continue;

            var segmentDir = segmentVec / segmentLength;
            var segmentAngle = segmentVec.ToWorldAngle();

            // Distance already drawn inside this segment
            var distDrawn = 0f;

            while (distDrawn < segmentLength - 0.001f)
            {
                // How much we can draw before hitting either a tile boundary or the segment end
                var remainingInTile = textureHeight - texOffset;
                var drawLength = Math.Min(remainingInTile, (float)(segmentLength - distDrawn));

                var startPos = segmentStart + segmentDir * distDrawn;
                var endPos = startPos + segmentDir * drawLength;
                var midPoint = (startPos + endPos) / 2f;

                var uvTop = texOffset * EyeManager.PixelsPerMeter;
                var uvBottom = (texOffset + drawLength) * EyeManager.PixelsPerMeter;
                var uv = new UIBox2(0, uvTop, texture.Width, uvBottom);

                // The box inside of which the texture will be drawn
                var box = new Box2(
                    -textureWidth / 2f,
                    -drawLength / 2f,
                    textureWidth / 2f,
                    drawLength / 2f);
                var rotatedBox = new Box2Rotated(
                    box.Translated(midPoint),
                    segmentAngle,
                    midPoint);

                worldHandle.DrawTextureRectRegion(texture, rotatedBox, color, uv);

                // Advance within the tile and the segment
                distDrawn += drawLength;
                texOffset = (texOffset + drawLength) % textureHeight;
            }
            // The next segment starts with the same texOffset (carry‑over of partial tile)
        }
    }

    /// <summary>
    ///     Calculates the world position of the anchor point for a connected entity.
    /// </summary>
    private Vector2 GetAnchorPosition(EntityUid entity, OverlayDrawArgs args)
    {
        if (!_xformQuery.TryGetComponent(entity, out var xform))
            return Vector2.Zero;

        var pos = _xform.ToMapCoordinates(xform.Coordinates, false).Position;
        var rot = _xform.GetWorldRotation(xform);

        var offset = Vector2.Zero;
        if (_spriteQuery.TryGetComponent(entity, out var sprite))
            offset = sprite.Offset * sprite.Scale;

        // TODO: might need to take joints into account since they can be created with offsets

        return pos + rot.RotateVec(offset);
    }
}
