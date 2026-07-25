namespace Content.Shared._Floof.InteractionVerbs;

[Flags]
public enum InteractionVerbSource
{
    /// Source is unknown. Verb will likely get ignored.
    Unknown = 1 << 0,
    /// This interaction verb is added from the global pool of verbs.
    Global = 1 << 1,
    /// This interaction verb is fascilated by the target's InteractionVerbsComponent.
    TargetVerbs = 1 << 2,
    /// This interaction verb is fascilated by the user's OwnInteractionVerbsComponent.
    UserVerbs = 1 << 3,
    /// This interaction verb is fascilated by a tool held by the target. Currently not implemented.
    ToolVerbs = 1 << 4,
    // Make sure to add new sources if you need them. Don't re-use without a reason.

    /// Meta, for use in yaml
    All = Global | TargetVerbs | UserVerbs | ToolVerbs
}
