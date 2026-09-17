using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Makes this rules antags spawn the player session's selected character.
/// </summary>
[RegisterComponent]
public sealed partial class AntagLoadPlayerCharacterRuleComponent : Component;
