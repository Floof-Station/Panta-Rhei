using Content.Client._HL.Brainwashing.CompulsionsUI;
using Content.Client.Eui;
using Content.Shared._HL.Brainwashing;
using Content.Shared.Eui;

namespace Content.Client._HL.Brainwashing;

public sealed class BrainwashViewer : BaseEui
{
    private readonly BrainwashMenu _brainwashMenu;

    public BrainwashViewer()
    {
        IoCManager.Resolve<EntityManager>();

        _brainwashMenu = new BrainwashMenu();
        _brainwashMenu.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is BrainwashViewerState s)
            _brainwashMenu.Populate(s.Compulsions);
    }

    public override void Opened()
    {
        _brainwashMenu.OpenCentered();
    }
}
