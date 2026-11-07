using Content.Client._HL.Brainwashing.CompulsionsUI;
using Content.Shared._HL.Brainwashing;
using Robust.Client.UserInterface;

namespace Content.Client._HL.Brainwashing;

public sealed class BrainwashViewerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BrainwashMenu? _brainwashMenu;

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is BrainwashViewerState s)
            _brainwashMenu?.Populate(s.Compulsions);
    }

    protected override void Open()
    {
        base.Open();
        _brainwashMenu = this.CreateWindow<BrainwashMenu>();;
    }
}
