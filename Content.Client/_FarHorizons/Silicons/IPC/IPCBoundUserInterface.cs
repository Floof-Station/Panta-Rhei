using Content.Shared._FarHorizons.Silicons.IPC;
using Robust.Client.UserInterface;

namespace Content.Client._FarHorizons.Silicons.IPC;

public sealed partial class IPCBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private IPCMenu? _menu;

    public IPCBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey){}

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<IPCMenu>();
        _menu.SetEntity(Owner);

        _menu.BrainButtonPressed += () =>
        {
            SendMessage(new IPCEjectBrainBuiMessage());
        };

        _menu.EjectBatteryButtonPressed += () =>
        {
            SendMessage(new IPCEjectBatteryBuiMessage());
        };

        _menu.NameChanged += name =>
        {
            SendMessage(new IPCSetNameBuiMessage(name));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not IPCBuiState msg)
            return;
        _menu?.UpdateState(msg);
    }
}