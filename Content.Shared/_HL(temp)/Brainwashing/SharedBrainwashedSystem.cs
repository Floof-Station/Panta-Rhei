using Content.Shared.Actions;

namespace Content.Shared._HL.Brainwashing;

public class SharedBrainwashedSystem : EntitySystem
{
    public bool SetCompulsions(EntityUid uid, BrainwashedComponent brainwashedComponent, List<string> compulsions)
    {
        brainwashedComponent.Compulsions = compulsions;
        DirtyField(uid, brainwashedComponent, nameof(brainwashedComponent.Compulsions));
        return true;
    }

    public bool SetCompulsions(EntityUid uid, BrainwasherComponent brainwasherComponent, List<string> compulsions)
    {
        brainwasherComponent.Compulsions = compulsions;
        DirtyField(uid, brainwasherComponent, nameof(brainwasherComponent.Compulsions));
        return true;
    }
}

public sealed partial class OpenCompulsionsMenuAction : InstantActionEvent;
