using Content.Shared._Floof.Lewd.Systems;

namespace Content.Shared._Floof.Lewd.Components;

/// <summary>
///     Applied to organs that are part of the lewd system.
///
///     Makes the body able to produce and/or store chemicals in special solutions.
///     The solutions are currently stored on the body.
/// </summary>
[RegisterComponent, Access(typeof(LewdOrganSystem))]
public sealed partial class LewdOrganComponent : Component
{
    [DataField]
    public LewdOrganData Data = new();
}
