using Content.Shared._Floof.Ropes.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Ropes.Systems;

public sealed partial class RopeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    private EntityQuery<RopeComponent> _ropeQuery;
    private EntityQuery<RopeLinkComponent> _ropeLinkQuery;

    public override void Initialize()
    {
        InitializeLifecycle();
        InitializeNetworking();

        _ropeQuery = GetEntityQuery<RopeComponent>();
        _ropeLinkQuery = GetEntityQuery<RopeLinkComponent>();
    }
}
