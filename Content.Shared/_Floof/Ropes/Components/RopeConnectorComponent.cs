using Content.Shared._Floof.Ropes.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Ropes.Components;

/// <summary>
///     Allows entities like rope bundles to be freely connected to other entities that pass the whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RopeConnectorComponent : Component
{
    [DataField(required: true)]
    public ProtoId<RopeConfigurationPrototype> RopePrototype;

    [DataField(required: true)]
    public EntProtoId HandlePrototype;

    [DataField(required: true)]
    public TimeSpan ConnectDelay;

    /// <summary>
    ///     Which ends can be connected by the user.
    /// </summary>
    [DataField]
    public List<Side> ConnectableSides = new() { Side.Start, Side.End };

    /// <summary>
    ///     If true, the rope can be "unrolled", spawning another handle on the other end.
    /// </summary>
    [DataField]
    public bool CanUnroll = true;

    [DataField]
    public float CurrentLength = 5f;

    [DataField]
    public EntityWhitelist? TargetWhitelist = null, TargetBlacklist = null;

    public EntityUid? RopeEntity, HandleEntity;

    public enum Side
    {
        /// Connects first. Can be unrolled into a second handle.
        End,
        /// Connects last. This side is held by the user at first.
        Start,

    }
}
