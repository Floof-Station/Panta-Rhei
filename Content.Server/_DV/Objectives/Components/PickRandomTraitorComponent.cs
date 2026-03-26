using Content.Server.Objectives.Systems;
using Content.Shared._Floof.Traits.Components;

namespace Content.Server._DV.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random traitor.
/// </summary>
[RegisterComponent]
public sealed partial class PickRandomTraitorComponent : Component
{
    [DataField, ViewVariables]
    public ObjectiveTypes TargetType; ///Euphoria | Holds data for Marked targetting in relation to Fellow Traitor objectives.
}
