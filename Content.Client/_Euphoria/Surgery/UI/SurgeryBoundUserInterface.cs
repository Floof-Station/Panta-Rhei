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
    }
}
