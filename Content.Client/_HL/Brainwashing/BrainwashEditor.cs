using Content.Client._HL.Brainwashing.CompulsionsUI.EditUI;
using Content.Client.Eui;
using Content.Shared._HL.Brainwashing;
using Content.Shared.Eui;

namespace Content.Client._HL.Brainwashing;

public sealed class BrainwashEditor : BaseEui
{
    private readonly EntityManager _entityManager;

    private readonly BrainwashEui _brainwashEui;
    private EntityUid _target;

    public BrainwashEditor()
    {
        _entityManager = IoCManager.Resolve<EntityManager>();

        _brainwashEui = new BrainwashEui();
        _brainwashEui.OnClose += () => SendMessage(new CloseEuiMessage());
        _brainwashEui.SaveButton.OnPressed += _ =>
        {
            var compulsions = _brainwashEui.GetCompulsions();
            var netEntity = _entityManager.GetNetEntity(_target);
            var brainwashSaveMessage = new BrainwashSaveMessage(compulsions, netEntity);
            SendMessage(brainwashSaveMessage);
        };
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not BrainwashEuiState s)
        {
            return;
        }

        _target = _entityManager.GetEntity(s.Target);
        _brainwashEui.SetCompulsions(s.Compulsions);
    }

    public override void Opened()
    {
        _brainwashEui.OpenCentered();
    }
}
