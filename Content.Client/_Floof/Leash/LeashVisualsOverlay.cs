using System.Numerics;
using Content.Shared._Floof.Leash.Components;
using Content.Shared._Floof.Paint;
using Content.Shared._Floof.Util;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._Floof.Leash;

public sealed class LeashVisualsOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly IEntityManager _entMan;
    private readonly IGameTiming _timing;
    private readonly SpriteSystem _sprites;
    private readonly SharedTransformSystem _xform;

    private readonly EntityQuery<TransformComponent> _xformQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<ColorPaintedComponent> _paintQuery;

    private ISawmill Log => Logger.GetSawmill("leash-visuals");
    private Ticker _logTicker = new(TimeSpan.FromSeconds(3));

    public LeashVisualsOverlay(IEntityManager entMan)
    {
        _entMan = entMan;
        _timing = IoCManager.Resolve<IGameTiming>();
        _sprites = _entMan.System<SpriteSystem>();
        _xform = _entMan.System<SharedTransformSystem>();
        _xformQuery = _entMan.GetEntityQuery<TransformComponent>();
        _spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();
        _paintQuery = _entMan.GetEntityQuery<ColorPaintedComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        worldHandle.SetTransform(Vector2.Zero, Angle.Zero);

        var query = _entMan.EntityQueryEnumerator<Shared._Floof.Leash.Components.LeashedVisualsComponent>();
        while (query.MoveNext(out var visualsComp))
        {
            if (visualsComp.Source is not {Valid: true} source
                || visualsComp.Target is not {Valid: true} target
                || !_xformQuery.TryGetComponent(source, out var sourceXform)
                || !_xformQuery.TryGetComponent(target, out var targetXform)
                || sourceXform.MapID != args.MapId
                || targetXform.MapID != sourceXform.MapID)
                continue;

            var texture = _sprites.Frame0(visualsComp.Sprite);
            var width = texture.Width / (float) EyeManager.PixelsPerMeter;

            var coordsA = sourceXform.Coordinates;
            var coordsB = targetXform.Coordinates;

            // If both coordinates are in the same spot (e.g. the leash is being held by the leashed), don't render anything
            if (coordsA.TryDistance(_entMan, _xform, coordsB, out var dist) && dist < 0.01f)
                continue;

            ExtractAnchorData(args, (source, sourceXform), visualsComp, out var rotA, out var offsetA);
            ExtractAnchorData(args, (target, targetXform), visualsComp, out var rotB, out var offsetB);

            coordsA = coordsA.Offset(rotA.RotateVec(offsetA));
            coordsB = coordsB.Offset(rotB.RotateVec(offsetB));

            var posA = _xform.ToMapCoordinates(coordsA).Position;
            var posB = _xform.ToMapCoordinates(coordsB).Position;
            var diff = (posB - posA);
            var length = diff.Length();
            var angle = (posB - posA).ToWorldAngle();

            // Source is always the leash as of now.
            // If it ever changes, make sure to change the visuals comp to include a reference to the leash.
            var color = _paintQuery.CompOrNull(source)?.Color;

            // We draw the leash as multiple segments
            var maxSegmentLength = texture.Height / (float)EyeManager.PixelsPerMeter;
            var segmentCount = Math.Max(1, (int)Math.Ceiling(length / maxSegmentLength));

            // Sanity check
            if (segmentCount > 16)
            {
                if (_logTicker.TryUpdate(_timing))
                    Log.Warning("Tried to render a leash joint with absurd length.");
                return;
            }

            // Note: we do not draw the last segment as part of this loop because it needs to be drawn partially.
            var direction = diff / length;
            for (var i = 0; i < segmentCount - 1; i++)
            {
                var segmentStart = posA + direction * maxSegmentLength * i;

                // So basically, we find the midpoint, then create a box that describes the sprite boundaries, then rotate it
                var segmentMidPoint = segmentStart + direction * (maxSegmentLength / 2f);
                var box = new Box2(-width / 2f, -maxSegmentLength / 2f, width / 2f, maxSegmentLength / 2f);
                var rotate = new Box2Rotated(box.Translated(segmentMidPoint), angle, segmentMidPoint);

                // Draw the segment
                worldHandle.DrawTextureRect(texture, rotate, color);
            }

            // Draw the last segment partially.
            // TODO: code duplication
            {
                var segmentStart = posA + direction * maxSegmentLength * (segmentCount - 1);
                var segmentLength = length - maxSegmentLength * (segmentCount - 1);
                var segmentMidPoint = segmentStart + direction * (segmentLength / 2f);
                // I can't really explain why things are done the way they are here, most of it was achieved by trial and error.
                var box = new Box2(-width / 2f, -segmentLength / 2f, width / 2f, segmentLength / 2f);
                var rotate = new Box2Rotated(box.Translated(segmentMidPoint), angle + Angle.FromDegrees(180), segmentMidPoint);

                // The texture is mirrored by swapping left and right uv coordinates
                var uv = new UIBox2(texture.Width, segmentLength * EyeManager.PixelsPerMeter, 0, 0);
                worldHandle.DrawTextureRectRegion(texture, rotate, color, uv);
            }
        }
    }

    private void ExtractAnchorData(OverlayDrawArgs args, Entity<TransformComponent> leashedEntity, LeashedVisualsComponent visualsComp, out Angle rotation, out Vector2 offset)
    {
        rotation = leashedEntity.Comp.LocalRotation;
        offset = visualsComp.OffsetSource;

        // NoRotation sprites always have a zero rotation, and their "up" is always facing the viewport "up"
        // Regular sprites on the other hand can have any rotation, and their rotation is described in world coordinates
        if (_spriteQuery.TryGetComponent(leashedEntity, out var sprite))
        {
            offset *= sprite.Scale;
            offset += sprite.Offset;
            if (sprite.NoRotation)
                rotation = -args.Viewport.Eye?.Rotation ?? Angle.Zero;
            else
                rotation = sprite.Rotation;
        }
    }
}
