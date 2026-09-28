using Content.Shared._Lavaland.Procedural.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Lavaland;

/// <summary>
///     A variant of StationPlanetSpawnerComponent that spawns a lavaland planet. Goobstation shitcode does it globally.
/// </summary>
[RegisterComponent]
public sealed partial class StationLavalandSpawnerComponent : Component
{
    [DataField]
    public ProtoId<LavalandMapPrototype> Prototype = "Lavaland";

    public EntityUid Planet;
}
