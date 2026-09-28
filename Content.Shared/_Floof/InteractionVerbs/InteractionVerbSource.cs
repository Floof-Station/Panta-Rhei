namespace Content.Shared._Floof.InteractionVerbs;

[Flags]
public enum InteractionVerbSource
{
    /// Source is unknown. Verb will likely get ignored.
    Unknown = 1 << 0,
    /// This interaction verb is added from the pool of global verbs
    Global = 1 << 1,
    /// This interaction verb is facilitated by the target's InteractionVerbsComponent or other components.
    TargetVerbs = 1 << 2,
    /// This interaction verb is facilitated by the user's OwnInteractionVerbsComponent or other components.
    UserVerbs = 1 << 3,
    /// This interaction verb is facilitated by a tool held by the target. Currently not implemented.
    ToolVerbs = 1 << 4,
    // Make sure to add new sources if you need them. Don't re-use without a reason.

    /// Meta, for use in yaml
    All = Global | TargetVerbs | UserVerbs | ToolVerbs,
    /// Meta, all except tool verbs (prevents holding a plushie allowing you to hug walls, for example)
    AllExceptTools = All & (~ToolVerbs),
}
