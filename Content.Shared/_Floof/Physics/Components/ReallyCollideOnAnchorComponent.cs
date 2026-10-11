using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Physics.Components;

/// <summary>
///     An extension for <see cref="CollideOnAnchorComponent"/> that also updates collision state after every collision change.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReallyCollideOnAnchorComponent : Component;
