    using Content.Shared._Floof.Vore;
    using Robust.Client.Graphics;
    using Robust.Client.Player;
    using Robust.Shared.GameObjects;
    using Content.Shared.GameTicking;
    using Robust.Shared.Player;

    namespace Content.Client._Floof.Vore;

    public sealed class DevouredBlindSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayMan = default!;
        [Dependency] private readonly IPlayerManager _player = default!;

        private DevouredOverlay _overlay = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<DevouredComponent, ComponentInit>(OnVoreInit);
            SubscribeLocalEvent<DevouredComponent, ComponentShutdown>(OnVoreShutdown);
            SubscribeLocalEvent<DevouredComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<DevouredComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
            _overlay = new DevouredOverlay();
        }

        private void OnPlayerAttached(EntityUid uid, DevouredComponent component, LocalPlayerAttachedEvent args)
        {
            _overlayMan.AddOverlay(_overlay);
        }

        private void OnPlayerDetached(EntityUid uid, DevouredComponent component, LocalPlayerDetachedEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }

        private void OnVoreInit(EntityUid uid, DevouredComponent component, ComponentInit args)
        {
            if (_player.LocalEntity == uid)
                _overlayMan.AddOverlay(_overlay);
        }

        private void OnVoreShutdown(EntityUid uid, DevouredComponent component, ComponentShutdown args)
        {
            if (_player.LocalEntity == uid)
                _overlayMan.RemoveOverlay(_overlay);
        }
    }