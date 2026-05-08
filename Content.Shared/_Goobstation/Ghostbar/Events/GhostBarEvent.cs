using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Ghostbar.Events;

/// <summary>
/// Goobstation - A server to client request for them to spawn at the ghost bar
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostBarSpawnEvent : EntityEventArgs;
