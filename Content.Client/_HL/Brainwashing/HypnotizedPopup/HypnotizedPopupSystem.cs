using Content.Shared._HL.Brainwashing;

namespace Content.Client._HL.Brainwashing.HypnotizedPopup;

public sealed class HypnotizedPopupSystem : EntitySystem
{
    private HypnotizedPopup? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<BrainwashedEvent>(OnBrainwashed);
    }

    private void OnBrainwashed(BrainwashedEvent args)
    {
        if (_window != null)
            return;

        _window = new HypnotizedPopup();
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }
}
