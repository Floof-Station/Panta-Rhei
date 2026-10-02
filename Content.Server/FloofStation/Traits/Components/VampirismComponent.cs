namespace Content.Server.Floofstation.Traits.Components;

/// <summary>
/// Replaces the entity's stomach with a stomach for digesting blood and
/// allows them to drain blood from other biological creatures.
/// Must be fully initialized before being added to a mob.
/// </summary>
[RegisterComponent]
public sealed partial class VampirismComponent : Component;
