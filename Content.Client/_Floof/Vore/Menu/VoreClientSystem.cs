using Content.Shared._Floof.Vore;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client._Floof.Vore.Menu;

public sealed class VoreClientSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<OpenVoreMenuEvent>(OnOpenMenu);
    }

    private void OnOpenMenu(OpenVoreMenuEvent ev)
    {
        var ui = _ui.GetUIController<VoreUIController>();
        ui.OpenMenu();
    }
}
