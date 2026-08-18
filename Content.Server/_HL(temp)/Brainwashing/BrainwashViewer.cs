using Content.Server.EUI;
using Content.Shared._HL.Brainwashing;
using Content.Shared.Eui;

namespace Content.Server._HL.Brainwashing;

public sealed class BrainwashViewer : BaseEui
{
    private List<string> _compulsions = [];

    public void UpdateCompulsions(BrainwashedComponent brainwashedComponent)
    {
        _compulsions = brainwashedComponent.Compulsions;
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new BrainwashViewerState(_compulsions);
    }
}
