using Content.Server.Actions;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Shared._HL.Brainwashing;

public sealed class BrainwashedSystem : SharedBrainwashedSystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<BrainwashedComponent, BrainwashedEvent>(OnBrainwashed);
        SubscribeLocalEvent<BrainwashedComponent, ToggleCompulsionsMenuAction>(ToggleCompulsionsMenu);
        SubscribeLocalEvent<BrainwashedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BrainwashedComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void ToggleCompulsionsMenu(EntityUid uid, BrainwashedComponent component, ToggleCompulsionsMenuAction args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;
        _userInterface.TryToggleUi(uid, CompulsionsUiKey.Key, actor.PlayerSession);
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

    private void OnBoundUIOpened(EntityUid uid, BrainwashedComponent component, BoundUIOpenedEvent args)
    {
        var state = new BrainwashViewerState(component.Compulsions);
        _userInterface.SetUiState(args.Entity, CompulsionsUiKey.Key, state);
    }

    private void OnComponentInit(EntityUid uid, BrainwashedComponent component, ComponentInit args)
    {
        if (component.Compulsions.Count != 0)
            component.Action ??= _actionsSystem.AddAction(uid, component.ActionPrototype);
    }
}
