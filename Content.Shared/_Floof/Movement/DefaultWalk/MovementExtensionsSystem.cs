using Content.Shared._Floof.CCVar;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._Floof.Movement.DefaultWalk;

public sealed class MovementExtensionsSystem : EntitySystem
{
    [Dependency] private readonly INetConfigurationManager _netCfg = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<UpdateMovementCVarsEvent>(OnUpdateCvars);
        // This may be unsafe, change this if SharedMoverController decides to subscribe on MindAdded/RemovedEvent
        // AFAIK upstream has events in the MindSystem that get raised whenever any mind gets attached or detached
        SubscribeLocalEvent<InputMoverComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<InputMoverComponent, MindRemovedMessage>(OnMindRemoved);

        // Client only: raise an UpdateMovementCVarsEvent when changing the cvars. The actual cvar changing is handled in KeyRebindTab.
        if (_net.IsClient)
            Subs.CVar(_cfg, FloofCCVars.DefaultWalk, _ => RaiseNetworkEvent(new UpdateMovementCVarsEvent()));
    }

    private void OnUpdateCvars(UpdateMovementCVarsEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid || !TryComp<InputMoverComponent>(uid, out var mover))
            return;

        mover.DefaultWalk = GetDefaultWalk(uid);
        Dirty(uid, mover);
    }

    private void OnMindAdded(Entity<InputMoverComponent> ent, ref MindAddedMessage args)
    {
        ent.Comp.DefaultWalk = GetDefaultWalk(ent.Owner);
        Dirty(ent);
    }

    private void OnMindRemoved(Entity<InputMoverComponent> ent, ref MindRemovedMessage args)
    {
        ent.Comp.DefaultWalk = false; // See below
        Dirty(ent);
    }

    /// <summary>
    ///     Checks whether the player behind the mob wants to walk by default. This is an expensive operation, so make sure to cache the results.
    /// </summary>
    public bool GetDefaultWalk(Entity<ActorComponent?> ent)
    {
        // Non-players default to running so NPCs don't suffer from walking
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        return _netCfg.GetClientCVar(ent.Comp.PlayerSession.Channel, FloofCCVars.DefaultWalk);
    }
}
