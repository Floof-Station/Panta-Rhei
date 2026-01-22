using System.Numerics;
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
                || !_xformQuery.TryGetComponent(source, out var xformComp)
                || !_xformQuery.TryGetComponent(target, out var otherXformComp)
                || xformComp.MapID != args.MapId
                || otherXformComp.MapID != xformComp.MapID)
                continue;

            var texture = _sprites.Frame0(visualsComp.Sprite);
            var width = texture.Width / (float) EyeManager.PixelsPerMeter;

            var coordsA = xformComp.Coordinates;
            var coordsB = otherXformComp.Coordinates;

            // If both coordinates are in the same spot (e.g. the leash is being held by the leashed), don't render anything
            if (coordsA.TryDistance(_entMan, _xform, coordsB, out var dist) && dist < 0.01f)
                continue;

            var rotA = xformComp.LocalRotation;
            var rotB = otherXformComp.LocalRotation;
            var offsetA = visualsComp.OffsetSource;
            var offsetB = visualsComp.OffsetTarget;

            // NoRotation sprites always have a zero rotation, and their "up" is always facing the viewport "up"
            // Regular sprites on the other hand can have any rotation, and their rotation is described in world coordinates
            if (_spriteQuery.TryGetComponent(source, out var spriteA))
            {
                offsetA *= spriteA.Scale;
                offsetA += spriteA.Offset;
                if (spriteA.NoRotation)
                    rotA = -args.Viewport.Eye?.Rotation ?? Angle.Zero;
                else
                    rotA = spriteA.Rotation;
            }
            if (_spriteQuery.TryGetComponent(target, out var spriteB))
            {
                offsetB *= spriteB.Scale;
                offsetB += spriteB.Offset;
                if (spriteB.NoRotation)
                    rotB = -args.Viewport.Eye?.Rotation ?? Angle.Zero;
                else
                    rotB = spriteB.Rotation;
            }

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
            // Disclaimer: the below was written with the help of an LLM, my original code could only handle drawing the leash as 1 segment.
            var maxSegmentLength = texture.Height / (float)EyeManager.PixelsPerMeter;
            int segmentCount = Math.Max(1, (int)Math.Ceiling(length / maxSegmentLength));

            // Sanity check
            if (segmentCount > 16)
            {
                if (_logTicker.TryUpdate(_timing))
                    Log.Warning("Tried to render a leash joint with absurd length.");
                return;
            }

            var direction = diff / length;
            for (var i = 0; i < segmentCount; i++)
            {
                var segmentStart = posA + direction * maxSegmentLength * i;
                var segmentLength = (i == segmentCount - 1) ? (length - maxSegmentLength * i) : maxSegmentLength;

                // So basically, we find the midpoint, then create a box that describes the sprite boundaries, then rotate it
                var segmentMidPoint = segmentStart + direction * (segmentLength / 2f);
                var box = new Box2(-width / 2f, -segmentLength / 2f, width / 2f, segmentLength / 2f);
                var rotate = new Box2Rotated(box.Translated(segmentMidPoint), angle, segmentMidPoint);

                // Draw the segment
                worldHandle.DrawTextureRect(texture, rotate, color);
            }
        }
    }
}
