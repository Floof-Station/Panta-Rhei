using Content.Shared._Floof.Ropes.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Ropes.Components;

/// <summary>
///     Allows entities like rope bundles to be freely connected to other entities that pass the whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RopeConnectorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<RopeConfigurationPrototype> RopePrototype;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId HandlePrototype;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan ConnectDelay;

    /// <summary>
    ///     Which ends can be connected by the user.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Side> ConnectableSides = new() { Side.Start, Side.End };

    /// <summary>
    ///     If true, the rope can be "unrolled", spawning another handle on the other end.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanUnroll = true;

    [DataField, AutoNetworkedField]
    public float CurrentLength = 5f;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? TargetWhitelist = null, TargetBlacklist = null;

    [DataField, AutoNetworkedField]
    public EntityUid? RopeEntity, HandleEntity;

    public enum Side
    {
        /// Connects first. Can be unrolled into a second handle.
        End,
        /// Connects last. This side is held by the user at first.
        Start,

    }
}
