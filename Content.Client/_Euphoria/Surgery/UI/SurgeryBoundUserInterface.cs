using Content.Shared._Euphoria.Surgery;
using Robust.Client.UserInterface;

namespace Content.Client._Euphoria.Surgery.UI;

public sealed partial class SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SurgeryWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SurgeryWindow>();
        _window.SetEntity(Owner);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SurgeryUpdateState msg:
                if (_window != null)
                    _window.Surgeries = msg.SurgeryActions;
                _window?.PopulateSurgeries();
                break;
        }
    }
}
