using Content.Shared._Floof.Lewd.Systems;
using Content.Shared._Floof.Util;
using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Lewd.Components;

/// <summary>
///     Stores a cached list of all lewd organs the mob has, for use in prediction and to simplify checks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LewdMobDataComponent : Component
{
    [DataField(serverOnly: true)]
    public Ticker UpdateInterval = new(TimeSpan.FromSeconds(5));

    /// <summary>
    ///     A set of flags containing all organ kinds this mob has.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly), Access(typeof(LewdOrganSystem))]
    public LewdOrganKind OrganKinds;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly), Access(typeof(LewdOrganSystem))]
    public Dictionary<LewdOrganKind, LewdOrganData> CachedData = new();

    /// <summary>
    ///     If set to true, the player has chosen to allow other players to perform lewd interactions on them regardless of clothing.
    ///     This is mostly used in interaction verbs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BypassClothingChecks = false;
}
