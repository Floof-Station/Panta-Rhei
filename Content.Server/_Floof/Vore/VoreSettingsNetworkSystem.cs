using Content.Shared._Floof.Vore;
namespace Content.Server._Floof.Vore;

public sealed class VoreSettingsNetworkSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeNetworkEvent<VoreSettingsChangedEvent>(OnVoreSettingsChanged);
    }

    /// <summary>
    /// Turns the network event into a local one for the purpose of better
    /// seperation between pred and prey system
    /// </summary>
    private void OnVoreSettingsChanged(VoreSettingsChangedEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession?.AttachedEntity is not { } uid)
            return;
        RaiseLocalEvent(uid, ev);
    }
}
