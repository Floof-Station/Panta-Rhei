namespace Content.Client._Euphoria.Surgery.UI;

public sealed class SurgeryBoundUserInterface : BoundUserInterface
{
    private SurgeryWindow? _window;

    public SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
    }
}
