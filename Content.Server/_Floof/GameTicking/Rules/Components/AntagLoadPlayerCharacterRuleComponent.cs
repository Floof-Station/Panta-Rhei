namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Attach to an entity with <see cref="AntagSelectionComponent"/> to have it spawn the player's currently selected character.
/// </summary>
[RegisterComponent]
public sealed partial class AntagLoadPlayerCharacterRuleComponent : Component;
