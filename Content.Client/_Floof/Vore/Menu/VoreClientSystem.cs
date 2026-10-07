using Content.Shared._Floof.Vore;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
namespace Content.Client._Floof.Vore.Menu;

public sealed class VoreClientSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<OpenVoreMenuEvent>(OnOpenMenu);
        SubscribeNetworkEvent<VoreSettingsEvent>(OnSettingsReceived);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _entityManager.EntityNetManager.SendSystemNetworkMessage(new VoreSettingsRequestEvent());
    }

    private void OnSettingsReceived(VoreSettingsEvent ev)
    {
        _ui.GetUIController<VoreUIController>().SetSettings(ev.SettingsYaml);
    }

    /// <summary>
    /// once the event from server has been received open the menu 
    /// </summary>
    private void OnOpenMenu(OpenVoreMenuEvent ev)
    {
        var ui = _ui.GetUIController<VoreUIController>();
        ui.OpenMenu();
    }
}
