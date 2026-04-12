using Content.Shared._Floof.Lewd.Systems;

namespace Content.Shared._Floof.Lewd.Components;

/// <summary>
///     Applied to the actual organs that are part of the lewd system.
/// </summary>
[RegisterComponent, Access(typeof(LewdOrganSystem))]
public sealed partial class LewdOrganComponent : Component
{
    [DataField]
    public LewdOrganData Data = new();
}
