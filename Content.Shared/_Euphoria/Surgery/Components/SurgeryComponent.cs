using Content.Shared.Body;
using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.Surgery.Components;

/// <summary>
///     Handles all surgery related effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryComponent : Component
{
    [DataField]
    public Dictionary<string, List<EntityUid>> DictOrgans = new();


}
