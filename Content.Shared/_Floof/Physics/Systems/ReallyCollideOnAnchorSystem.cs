using Content.Shared._Floof.Physics.Components;
using Content.Shared.Construction.EntitySystems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Floof.Physics.Systems;

public sealed class ReallyCollideOnAnchorSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private EntityQuery<CollideOnAnchorComponent> _collideAnchorQuery;
    private EntityQuery<ReallyCollideOnAnchorComponent> _reallyCollideQuery;
    private HashSet<EntityUid> _toUpdate = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChanged);

        _collideAnchorQuery = GetEntityQuery<CollideOnAnchorComponent>();
        _reallyCollideQuery = GetEntityQuery<ReallyCollideOnAnchorComponent>();
    }

    private void OnCollisionChanged(ref CollisionChangeEvent args)
    {
        // For some fuckass reason this event is only ever raised broadcast
        if (!_reallyCollideQuery.HasComp(args.BodyUid))
            return;

        if (!_collideAnchorQuery.TryComp(args.BodyUid, out var collideOnAnchor))
        {
            Log.Error("ReallyCollideOnAnchor added to entity without CollideOnAnchor");
            return;
        }

        var xform = Transform(args.BodyUid);
        var enabled = ShouldEnable(xform, collideOnAnchor);
        Log.Info($"Should enable: {enabled}, is enabled: {args.CanCollide}");

        // Defer until the next tick to avoid recursion
        if (enabled != args.CanCollide)
            _toUpdate.Add(args.BodyUid);
    }

    public override void Update(float frameTime)
    {
        foreach (var uid in _toUpdate)
        {
            if (!_collideAnchorQuery.TryComp(uid, out var collideOnAnchor))
                continue;

            var xform = Transform(uid);
            var enabled = ShouldEnable(xform, collideOnAnchor);
            Log.Info($"Should enable: {enabled}, trying now");

            _physics.SetCanCollide(uid, enabled, force: false);
        }
        _toUpdate.Clear();
    }

    private static bool ShouldEnable(TransformComponent xform, CollideOnAnchorComponent collideOnAnchor) =>
        !xform.Anchored ^ collideOnAnchor.Enable;
}
