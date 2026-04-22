using Content.Shared._Floof.Lewd.Systems;
using Content.Shared._Floof.Util;
using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Lewd.Components;

/// <summary>
///     Stores a cached list of all lewd organs the mob has, for use in prediction and to simplify checks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(LewdOrganSystem))]
public sealed partial class LewdMobDataComponent : Component
{
    [DataField(serverOnly: true), Access(Other = AccessPermissions.ReadWriteExecute)]
    public Ticker UpdateInterval = new(TimeSpan.FromSeconds(5));

    /// <summary>
    ///     A set of flags containing all organ kinds this mob has.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public LewdOrganKind OrganKinds;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<LewdOrganKind, LewdOrganData> CachedData = new();
}
