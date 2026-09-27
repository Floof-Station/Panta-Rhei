using Content.Server.Stack;
using Content.Shared.Stacks;
using Robust.Shared.Timing;

namespace Content.Server._Euphoria.Stack;

public sealed class StackDecaySystem : EntitySystem
{
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StackDecayComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StackDecayComponent, StackSplitEvent>(OnStackSplit);
        SubscribeLocalEvent<StackDecayComponent, StackCountChangedEvent>(OnStackCountChange);
    }

    private void OnMapInit(Entity<StackDecayComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<StackComponent>(ent, out var stack))
            return;

        ent.Comp.LastTickCount = stack.Count;
        ent.Comp.LastTickTime = _timing.CurTime;
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnStackCountChange(Entity<StackDecayComponent> ent, ref StackCountChangedEvent args)
    {
        ent.Comp.LastTickCount = args.NewCount;
    }

    private void OnStackSplit(Entity<StackDecayComponent> ent, ref StackSplitEvent args)
    {
        if (!TryComp<StackComponent>(ent, out var stack) || !TryComp<StackComponent>(args.NewId, out var otherStack))
            return;

        ent.Comp.LastTickCount = stack.Count;

        var otherDecay = EnsureComp<StackDecayComponent>(args.NewId);
        otherDecay.HalfLifeTime = ent.Comp.HalfLifeTime;
        otherDecay.UpdateInterval = ent.Comp.UpdateInterval;
        otherDecay.LastTickTime = ent.Comp.LastTickTime;
        otherDecay.LastTickCount = otherStack.Count;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<StackComponent, StackDecayComponent>();
        while (query.MoveNext(out var uid, out var stack, out var decay))
        {
            if (_timing.CurTime < decay.NextUpdate)
                continue;
            decay.NextUpdate = _timing.CurTime + decay.UpdateInterval;

            if (decay.LastTickTime == TimeSpan.Zero || decay.LastTickCount < 0f)
            {
                // Can't decay yet, first tick
                decay.LastTickTime = _timing.CurTime;
                decay.LastTickCount = stack.Count;
                continue;
            }

            // Radioactive decay formula: N(t) = N0 * 1/2 ^ (t / T1.2)
            var lambda = (_timing.CurTime - decay.LastTickTime) / decay.HalfLifeTime;
            var currentAmount = decay.LastTickCount * MathF.Pow(0.5f, (float) lambda);
            var currentAmountRounded = Math.Max((int) MathF.Round(currentAmount), 0);

            if (currentAmountRounded == stack.Count)
                continue;

            _stacks.SetCount(uid, currentAmountRounded, stack);
            decay.LastTickTime = _timing.CurTime;
            decay.LastTickCount = currentAmount;
        }
    }
}
