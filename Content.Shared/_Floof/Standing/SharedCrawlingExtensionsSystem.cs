using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Floof.Standing;

/// <summary>
/// Floofstation extensions to the upstream crawling systems.
///
/// Handles requests to change whether a mob is currently "crawling under furniture".
/// The draw depth is actually changed in the client-side counterpart.
/// </summary>
public sealed class SharedCrawlingExtensionsSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleCrawlingUnder, InputCmdHandler.FromDelegate(HandleCrawlUnderRequest, handle: true))
            .Bind(ContentKeyFunctions.ToggleCrawlingDirection, InputCmdHandler.FromDelegate(HandleCrawlingToggleRequest, handle: true))
            .Register<SharedCrawlingExtensionsSystem>();

        SubscribeLocalEvent<CrawlingExtensionsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<CrawlingExtensionsComponent, DownedEvent>(OnDowned);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<CrawlingExtensionsComponent>();
    }

    private void HandleCrawlUnderRequest(ICommonSession? session)
    {
        if (session == null
            || session.AttachedEntity is not {} uid
            || !TryComp<StandingStateComponent>(uid, out var standingState)
            || !TryComp<CrawlingExtensionsComponent>(uid, out var underCrawl)
            || !_actionBlocker.CanConsciouslyPerformAction(uid)) // Floof - replaced CanInteract with consciousness
            return;

        var newState = !underCrawl.IsCrawlingUnder;
        if (standingState.Standing)
            newState = false; // If the entity is already standing, this function only serves a fallback method to fix its draw depth

        underCrawl.IsCrawlingUnder = newState;
        _speed.RefreshMovementSpeedModifiers(uid);
        Dirty(uid, underCrawl);
    }

    private void HandleCrawlingToggleRequest(ICommonSession? session)
    {
        if (session == null
            || session.AttachedEntity is not {} uid
            || !TryComp<StandingStateComponent>(uid, out var standingState)
            || !TryComp<CrawlingExtensionsComponent>(uid, out var underCrawl)
            || !_actionBlocker.CanConsciouslyPerformAction(uid)) // Floof - replaced CanInteract with consciousness
            return;
    }

    private void OnRefreshMovementSpeed(Entity<CrawlingExtensionsComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_standing.IsDown(ent.Owner))
            return;

        var modifier = ent.Comp.IsCrawlingUnder ? ent.Comp.CrawlingUnderSpeedModifier : 1f;
        args.ModifySpeed(modifier, modifier);
    }

    private void OnDowned(Entity<CrawlingExtensionsComponent> ent, ref DownedEvent args)
    {
        // By default, after downing, a mob should NOT be drawn under furniture
        if (_timing is { ApplyingState: false, IsFirstTimePredicted: true })
            ent.Comp.IsCrawlingUnder = false;
    }
}
