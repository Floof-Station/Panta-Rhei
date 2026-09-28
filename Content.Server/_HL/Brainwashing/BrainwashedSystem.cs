using Content.Server.Actions;
using Content.Server.EUI;
using Content.Server._HL.Brainwashing;
using Robust.Server.Player;

namespace Content.Shared._HL.Brainwashing;
//ERROR SO YOU REMEMBER TO MOVE THIS FILE

public sealed class BrainwashedSystem : SharedBrainwashedSystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    private BrainwashViewer _brainwashViewer = new BrainwashViewer();
    public override void Initialize()
    {
        SubscribeLocalEvent<BrainwashedComponent, BrainwashedEvent>(OnBrainwashed);
        SubscribeLocalEvent<BrainwashedComponent, ToggleCompulsionsMenuAction>(ToggleCompulsionsMenu);
    }

    private void ToggleCompulsionsMenu(EntityUid uid, BrainwashedComponent component, ToggleCompulsionsMenuAction args)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;
        if (!component.ViewingCompulsions)
        {
            // brainwashViewer.CompulsionWindow =
            //the window is opened
            _euiManager.OpenEui(_brainwashViewer, session);
            component.ViewingCompulsions = true;
        }
        else
        {
            //the window is closed
            _euiManager.CloseEui(_brainwashViewer);
            component.ViewingCompulsions = false;
        }
        _brainwashViewer.UpdateCompulsions(component);
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
