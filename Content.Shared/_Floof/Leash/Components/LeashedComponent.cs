namespace Content.Shared._Floof.Leash.Components;

[RegisterComponent]
public sealed partial class LeashedComponent : Component
{
    public const string VisualsContainerName = "leashed-visuals";

    [DataField]
    public string? JointId = null;

    [NonSerialized]
    public EntityUid? Leash = null, Anchor = null;
}
