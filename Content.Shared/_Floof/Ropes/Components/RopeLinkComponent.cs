using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Ropes.Components;

/// <summary>
///     Applied to rope links to automatically refresh joints.
/// </summary>
[RegisterComponent]
public sealed partial class RopeLinkComponent : Component
{
    [DataField]
    public EntityUid Rope;
}
