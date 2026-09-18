using System.Linq;
using System.Numerics;
using Content.Shared._Floof.Ropes.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.Core.Tokens;

namespace Content.Shared._Floof.Ropes.Components;

/// <summary>
///     Contains data about a rope - an array of entities (links) connected into a chain via distance joints.
///     The ends of a rope can be connected to different entities.
///
///     Under certain circumstances, such as when both ends of a rope are connected to entities inside the same storage,
///     the links of the rope may become temporarily disconnected from the relevant entities and sent to nullspace.
///
///     Entities of this type are always created at runtime.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(Systems.RopeSystem), typeof(State))]
public sealed partial class RopeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<RopeConfigurationPrototype> Configuration;

    /// <summary>
    ///     Entities to which the rope is connected on the start and end, as well as the IDs of their respective joints.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public AnchorInfo? ConnectedStart, ConnectedEnd;

    /// <summary>
    ///     List of all links this rope is made of.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<Link> Links = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public float RopeLength, LinkLength, LinkStiffness;

    /// <summary>
    ///     Link count, overrides the prototype if set.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int LinkCount = -1;

    /// <summary>
    ///     Optional color tint for the rope sprite.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Color? Color;

    /// <summary>
    ///     True if the links of this rope have been temporarily sent to nullspace for preservation while both entities are in the same container.
    ///     Links and anchors might have invalid joint IDs assigned to them.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsDisabled;

    public sealed class Link
    {
        /// <summary>
        ///     The entity that represents this link.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public EntityUid LinkEntity;

        /// <summary>
        ///     IDs of joints that connect this link to the ones to the left and right.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? LeftJoint, RightJoint;
    }

    public record struct AnchorInfo
    {
        public EntityUid Anchor;
        public string JointId;
        public Vector2 Offset;

        public AnchorInfo(EntityUid anchor, string jointId, Vector2 offset)
        {
            Anchor = anchor;
            JointId = jointId;
            Offset = offset;
        }
    }

    // Serializable state
    // I don't want to shove this shit into the the system, fuck that
    [Serializable, NetSerializable]
    public sealed partial class State : ComponentState
    {
        public ProtoId<RopeConfigurationPrototype> Configuration;
        public (NetEntity Anchor, string JointId, Vector2 Offset)? ConnectedStart, ConnectedEnd;
        public List<LinkState> Links;
        public float RopeLength, LinkLength, LinkStiffness;
        public int LinkCount;
        public Color? Color;
        public bool IsTemporarilyNullspaced;

        /// Creates a new state from a component
        public State(RopeComponent comp, IEntityManager entMan)
        {
            Configuration = comp.Configuration;
            if (comp.ConnectedStart is {} start)
                ConnectedStart = (entMan.GetNetEntity(start.Anchor), start.JointId, start.Offset);
            if (comp.ConnectedEnd is {} end)
                ConnectedEnd = (entMan.GetNetEntity(end.Anchor), end.JointId, end.Offset);
            Links = comp.Links.Select(it => new LinkState(it, entMan)).ToList();
            RopeLength = comp.RopeLength;
            LinkLength = comp.LinkLength;
            LinkStiffness = comp.LinkStiffness;
            LinkCount = comp.LinkCount;
            Color = comp.Color;
            IsTemporarilyNullspaced = comp.IsDisabled;
        }

        /// Applies this state to the component
        public void Apply(RopeComponent comp, EntityManager entMan)
        {
            comp.Configuration = Configuration;
            comp.ConnectedStart = ConnectedStart is { } start ? new(entMan.GetEntity(start.Anchor), start.JointId, start.Offset) : null;
            comp.ConnectedEnd = ConnectedEnd is { } end ? new(entMan.GetEntity(end.Anchor), end.JointId, end.Offset) : null;
            comp.Links = Links.Select(it => it.ToLink(entMan)).ToList();
            comp.RopeLength = RopeLength;
            comp.LinkLength = LinkLength;
            comp.LinkStiffness = LinkStiffness;
            comp.LinkCount = LinkCount;
            comp.Color = Color;
            comp.IsDisabled = IsTemporarilyNullspaced;
        }

        [Serializable, NetSerializable]
        public sealed partial class LinkState
        {
            public NetEntity LinkEntity;
            public string? LeftJoint, RightJoint;

            public LinkState(Link link, IEntityManager entMan)
            {
                LinkEntity = entMan.GetNetEntity(link.LinkEntity);
                LeftJoint = link.LeftJoint;
                RightJoint = link.RightJoint;
            }

            public Link ToLink(IEntityManager entMan) => new Link
            {
                LinkEntity = entMan.GetEntity(LinkEntity),
                LeftJoint = LeftJoint,
                RightJoint = RightJoint
            };
        }
    }
}
