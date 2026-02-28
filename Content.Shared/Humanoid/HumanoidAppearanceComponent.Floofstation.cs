namespace Content.Shared.Humanoid;

// Floofstation-specific additions to the humanoid appearence component
public sealed partial class HumanoidAppearanceComponent
{
    /// <summary>
    /// The specific markings that are hidden, whether or not the layer is hidden.
    /// This is so we can just turn off a single marking, or part of a single marking.
    /// (cus underwear, its for underwear, so you can take off your bra and still have your shirt on)
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> HiddenMarkings = new();
}
