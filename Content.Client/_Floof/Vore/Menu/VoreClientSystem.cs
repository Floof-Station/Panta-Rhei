using Content.Shared._Floof.Vore;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Utility;
namespace Content.Client._Floof.Vore.Menu;

public sealed class VoreClientSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IResourceManager _resources = default!;

    private static readonly ResPath SettingsPath = new("/vore_settings.yml");
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("vore.settings");
        SubscribeNetworkEvent<OpenVoreMenuEvent>(OnOpenMenu);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        var yaml = LoadSettings();
        _ui.GetUIController<VoreUIController>().SetSettings(yaml);
    }

    private string LoadSettings()
    {
        if (!_resources.UserData.Exists(SettingsPath))
            return "{}";

        try
        {
            return _resources.UserData.ReadAllText(SettingsPath);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load local Vore settings: {e}");
            return "{}";
        }
    }

    private void OnOpenMenu(OpenVoreMenuEvent ev)
    {
        var ui = _ui.GetUIController<VoreUIController>();
        ui.OpenMenu();
    }
}
