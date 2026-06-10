using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.MagicalCommand;

/// <summary>
/// This is the component that allows the blank pendant to be turned into its completed form when enough magical pendants are used on it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerOfFriendshipComponent : Component
{
    /// <summary>
    /// How many unique 'contributors' the amulet needs before it can power up.
    /// </summary>
    [DataField]
    public int ContributorsRequired = 3;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Contributors = [];

    /// <summary>
    /// What this item will turn into when the contributors list is full.
    /// </summary>
    [DataField]
    public EntProtoId EmpoweredResult;


    /// <summary>
    /// Effectively the 'group' that items must be in. By default, Syndicate amulets cannot charge the command amulets.
    /// </summary>
    [DataField]
    public string Keyword = "pure";
}

/// <summary>
/// This is the component that allows an amulet to power up something with <see cref="PowerOfFriendshipComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FriendshipContributorComponent : Component
{
    [DataField]
    public string Keyword = "pure";
}

/// <summary>
/// Currently used for making only the empowered amulet able to purify the Syndie one without having to make another tool prototype.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmpoweredFriendshipComponent : Component
{
}
