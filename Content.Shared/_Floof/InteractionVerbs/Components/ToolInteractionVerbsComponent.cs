using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.InteractionVerbs.Components;

/// <summary>
///     Makes this entity provide interaction verbs when held by its user in the main hand.
///     This component adds verbs with <see cref="InteractionVerbSource.ToolVerbs"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToolInteractionVerbsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<InteractionVerbPrototype>> AllowedVerbs = new();
}
