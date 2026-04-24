using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.MagicalCommand;

/// <summary>
/// This is the component that allows the blank pendant to be turned into its completed form when enough magical pendants are used on it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerOfFriendshipComponent : Component
{
    /// <summary>
    /// How many unique 'contributors' the amulet needs before it can power up.
    /// </summary>
    [DataField]
    public int ContributorsRequired = 3;

    [DataField]
    public List<EntityUid> Contributors = [];

    [DataField]
    public EntProtoId EmpoweredResult;
}
