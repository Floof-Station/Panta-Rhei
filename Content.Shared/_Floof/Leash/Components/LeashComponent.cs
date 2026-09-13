using Content.Shared._Floof.Ropes.Prototypes;
using Content.Shared._Floof.Util;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Leash.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LeashComponent : Component
{
    /// <summary>
    ///     Maximum number of ropes this leash can create.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxJoints = 1;

    public float CurrentLength = 3f;

    /// <summary>
    ///     The time it takes for one entity to attach/detach the leash to/from another entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(2f), DetachDelay = TimeSpan.FromSeconds(2f);

    /// <summary>
    ///     The time it takes for the leashed entity to detach itself from this leash wihout holding it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SelfDetachDelay = TimeSpan.FromSeconds(8f);

    /// <summary>
    ///     Interval at which the holder of the leash can pull attached entities closer to itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Ticker PullInterval = new(TimeSpan.FromSeconds(0.5f));

    /// <summary>
    ///     List of all joints and their respective pulled entities created by this leash.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<LeashData> Leashed = new();

    public ProtoId<RopeConfigurationPrototype> RopeConfig = "Leash10";

    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class LeashData
    {
        /// <summary>
        ///     ID of the rope data entity representing this leash. Can be null if it hasn't been created yet.
        /// </summary>
        [DataField]
        public NetEntity? Rope;

        /// <summary>
        ///     The entity attached to this leash. NOT the anchor.
        /// </summary>
        [DataField]
        public NetEntity Pulled;

        public LeashData(NetEntity? rope, NetEntity pulled)
        {
            Rope = rope;
            Pulled = pulled;
        }
    }
}
