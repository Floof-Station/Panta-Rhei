using Content.Shared.Body;
using Robust.Shared.GameStates;

namespace Content.Shared._Euphoria.Surgery;

/// <summary>
///     Handles all surgery related effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SurgerySystem))]
public sealed partial class SurgeryComponent : Component
{
    [DataField]
    public Dictionary<string, List<EntityUid>> DictOrgans = new();
}
