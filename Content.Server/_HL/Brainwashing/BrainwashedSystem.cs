using Content.Server.Actions;
using Content.Server.EUI;
using Content.Shared._HL.Brainwashing;
using Robust.Server.Player;

namespace Content.Server._HL.Brainwashing;

public sealed class BrainwashedSystem : SharedBrainwashedSystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<BrainwashedComponent, BrainwashedEvent>(OnBrainwashed);
        SubscribeLocalEvent<BrainwashedComponent, OpenCompulsionsMenuAction>(OpenCompulsionsMenu);
    }

    private void OpenCompulsionsMenu(EntityUid uid, BrainwashedComponent component, OpenCompulsionsMenuAction args)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;
        var brainwashViewer = new BrainwashViewer();
        _euiManager.OpenEui(brainwashViewer, session);
        brainwashViewer.UpdateCompulsions(component);
    }

    private void OnBrainwashed(EntityUid uid, BrainwashedComponent component, BrainwashedEvent args)
    {
        if (component.Compulsions.Count != 0)
            component.Action ??= _actionsSystem.AddAction(uid, component.ActionPrototype);
        else
        {
            _actionsSystem.RemoveAction(uid, component.Action);
            component.Action = null; // Redundant, but why not.
            RemComp<BrainwashedComponent>(uid);
        }
    }
}
