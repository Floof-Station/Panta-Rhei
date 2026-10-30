using Content.Shared._Floof.Vore;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Client.State;
using Content.Client.Gameplay;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controllers.Implementations;
namespace Content.Client._Floof.Vore.Menu;

[UsedImplicitly]
public sealed class VoreUIController : UIController, IOnStateChanged<GameplayState>
{
    private VoreMenu? _menu;

    public void OnStateEntered(GameplayState state)
    {
        _menu = new VoreMenu();
        _menu.LoadSettings();
        _menu.SendSettingsChangedEvent();
    }

    public void OnStateExited(GameplayState state)
    {
        _menu?.Dispose();
        _menu = null;
    }

    public void OpenMenu()
    {
        if (_menu == null)
            return;
        if (!_menu.IsOpen)
            _menu.OpenCentered();
    }

}