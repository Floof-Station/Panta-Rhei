using Content.Shared._FarHorizons.Silicons.IPC.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.StatusEffects.IPCFanStop;

public abstract class SharedIPCFanStopStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<IPCThermalRegulationComponent, AttemptAddStatusEvent>(OnAttemptAdd);
        // SubscribeLocalEvent<IPCThermalRegulationComponent, AttemptRemoveStatusEvent>(OnAttemptRemoved);
        SubscribeLocalEvent<IPCFanStopStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<IPCFanStopStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
    }

    // Euph - we dont have this shit and it seems like unnecessary ai-generated shitcode.
    // private void OnAttemptAdd(Entity<IPCThermalRegulationComponent> ent, ref AttemptAddStatusEvent args)
    // {
    //     if(args.Effect != "StatusEffectIPCFanDisabled")
    //         return;
    //     ent.Comp.StoppedFanSources+=1;
    //     Dirty(ent);
    //
    // }
    //
    // private void OnAttemptRemoved(Entity<IPCThermalRegulationComponent> ent, ref AttemptRemoveStatusEvent args)
    // {
    //     if(args.Effect != "StatusEffectIPCFanDisabled")
    //         return;
    //     ent.Comp.StoppedFanSources-=1;
    //     Dirty(ent);
    //
    //     if(ent.Comp.StoppedFanSources > 0)
    //         args.Cancelled = true;
    // }

    private void OnEffectApplied(Entity<IPCFanStopStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!_gameTiming.ApplyingState)
        {
            if (TryComp<IPCThermalRegulationComponent>(args.Target, out var thermals))
            {
                thermals.FansOffOverride = true;
                Dirty(args.Target, thermals);
            }
        }
    }
    private void OnEffectRemoved(Entity<IPCFanStopStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TryComp<IPCThermalRegulationComponent>(args.Target, out var thermals))
            {
                thermals.FansOffOverride = false;
                Dirty(args.Target, thermals);
            }
    }
}
