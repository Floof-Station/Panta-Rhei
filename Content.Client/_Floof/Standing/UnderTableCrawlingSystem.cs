using Content.Shared._Floof.Standing;
using Content.Shared.Standing;
using Robust.Client.GameObjects;

namespace Content.Client._Floof.Standing;

/// <summary>
/// Handles updating the draw depths of mobs.
/// </summary>
public sealed class UnderTableCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprites = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<UnderTableCrawlingComponent, StandingStateComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var underTable, out var standing, out var sprite))
        {
            // Do not modify the entities draw depth if it's modified externally
            if (sprite.DrawDepth != underTable.NormalDrawDepth && sprite.DrawDepth != underTable.CrawlingUnderDrawDepth)
                continue;

            var depth = (!standing.Standing && underTable.IsCrawlingUnder)
                ? underTable.CrawlingUnderDrawDepth
                : underTable.NormalDrawDepth;
            _sprites.SetDrawDepth((uid, sprite), depth);
        }

        query.Dispose();
    }
}
