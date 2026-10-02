using Content.Server.EUI;
using Content.Shared._HL.Brainwashing;
using Content.Shared.Eui;

namespace Content.Server._HL.Brainwashing;

public sealed class BrainwashEditor(SharedBrainwashedSystem sharedBrainwashedSystem) : BaseEui
{
    private readonly EntityManager _entityManager = IoCManager.Resolve<EntityManager>();
    private EntityUid _target;
    private List<string> _compulsions = [];

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not BrainwashSaveMessage message)
            return;

        var entity = _entityManager.GetEntity(message.Target);
        _entityManager.TryGetComponent<BrainwasherComponent>(entity, out var brainwasherComponent);
        if (brainwasherComponent != null)
            sharedBrainwashedSystem.SetCompulsions(entity, brainwasherComponent, message.Compulsions);
    }

    public void UpdateCompulsions(BrainwasherComponent brainwashedComponent, EntityUid entity)
    {
        _compulsions = brainwashedComponent.Compulsions;
        _target = entity;
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new BrainwashEuiState(_compulsions, _entityManager.GetNetEntity(_target));
    }
}
