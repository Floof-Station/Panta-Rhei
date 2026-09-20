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

    // Not implemented
    // [DataField(required: true), AutoNetworkedField]
    // public EntProtoId HandlePrototype;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan ConnectDelay;

    /// <summary>
    ///     Which ends can be connected by the user.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<RopeSide> ConnectableSides = new() { RopeSide.Start, RopeSide.End };

    /// <summary>
    ///     If true, the rope can be "unrolled", spawning another handle on the other end.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanUnroll = true;

    [DataField, AutoNetworkedField]
    public float CurrentLength = 5f;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? TargetWhitelist = null, TargetBlacklist = null;

    // Note: handles were never implemented
    [DataField, AutoNetworkedField]
    public EntityUid? RopeEntity, HandleEntity;

    /// <summary>
    ///     Which side of the rope is connected to the connector.
    ///     This side cannot be connected until the other side is connected.
    ///     Once the master side is connected, the connector is taken out of user's hands and placed inside a container on the target entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RopeSide MasterSide = RopeSide.Start;
}
