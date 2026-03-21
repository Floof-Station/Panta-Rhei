using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Movement.DefaultWalk;

/// <summary>
///     Raised client->server whenever the client changes its "default walk" cvar or other movement settings.
/// </summary>
[Serializable, NetSerializable]
public sealed class UpdateMovementCVarsEvent : EntityEventArgs { }
