using Robust.Shared.Utility;

namespace Content.Shared._Floof.Ropes.Components;

/// <summary>
///     Added to entities that have one or more ropes attached to them.
/// </summary>
[RegisterComponent]
public sealed partial class RopeAttachedComponent : Component
{
    [DataField]
    public List<AttachedRopeInfo> AttachedRopes = new();

    /// <summary>
    ///     Returns the index of the element of AttachedRopes that corresponds to the given rope (there should only ever be one).
    /// </summary>
    public int IndexOfRope(EntityUid rope)
    {
        for (var i = 0; i < AttachedRopes.Count; i++)
        {
            if (AttachedRopes[i].Rope == rope)
                return i;
        }
        return -1;
    }

    /// <summary>
    ///     Removes info about the given rope.
    /// </summary>
    public AttachedRopeInfo? RemoveRope(EntityUid rope)
    {
        for (var i = 0; i < AttachedRopes.Count; i++)
        {
            if (AttachedRopes[i].Rope != rope)
                continue;

            return AttachedRopes.RemoveSwap(i);
        }
        return null;
    }

    [DataDefinition]
    public sealed partial class AttachedRopeInfo
    {
        [DataField]
        public EntityUid Rope;

        /// <summary>
        ///     If the rope is an item that's held in someone's hand, the connection is relayed to all entities in this field.
        /// </summary>
        [DataField]
        public List<EntityUid>? RelayedTo;

        /// <summary>
        ///     If this "attachment" is the result of a held rope being relayed, this field contains the actual item that is attached.
        /// </summary>
        [DataField]
        public EntityUid? RelayedFrom;

        public AttachedRopeInfo(EntityUid rope, List<EntityUid>? relayedTo, EntityUid? relayedFrom)
        {
            Rope = rope;
            RelayedTo = relayedTo;
            RelayedFrom = relayedFrom;
        }
    }
}
