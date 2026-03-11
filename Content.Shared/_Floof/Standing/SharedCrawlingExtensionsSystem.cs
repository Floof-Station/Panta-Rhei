using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Content.Shared.Rotation;
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
    [Dependency] private readonly SharedRotationVisualsSystem _rotVisuals = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearence = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleCrawlingUnder, InputCmdHandler.FromDelegate(HandleCrawlUnderRequest, handle: false))
            .Bind(ContentKeyFunctions.ToggleCrawlingDirection, InputCmdHandler.FromDelegate(HandleDirectionToggleRequest, handle: false))
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
            || !TryComp<CrawlingExtensionsComponent>(uid, out var ext)
            || !_actionBlocker.CanConsciouslyPerformAction(uid)
            || !_timing.IsFirstTimePredicted
            || !ext.CanCrawlUnderTables)
            return;

        var newState = !ext.IsCrawlingUnder;
        if (standingState.Standing)
            newState = false; // If the entity is already standing, this function only serves a fallback method to fix its draw depth

        ext.IsCrawlingUnder = newState;
        _speed.RefreshMovementSpeedModifiers(uid);
        Dirty(uid, ext);
    }

    private void HandleDirectionToggleRequest(ICommonSession? session)
    {
        if (session == null
            || session.AttachedEntity is not {} uid
            || !TryComp<StandingStateComponent>(uid, out var standingState)
            || !TryComp<CrawlingExtensionsComponent>(uid, out var ext)
            || !TryComp<AppearanceComponent>(uid, out var appearance)
            || !_actionBlocker.CanConsciouslyPerformAction(uid)
            || !_timing.IsFirstTimePredicted
            || !ext.CanChangeDirections
            || !ext.CrawlingDirectionChangeCooldown.TryUpdate(_timing))
            return;

        ext.InvertedCrawlingDirection = !ext.InvertedCrawlingDirection;
        Dirty(uid, ext);

        // +90deg = default horizontal rotation, -90deg = opposite
        var rotVisuals = EnsureComp<RotationVisualsComponent>(uid);
        _rotVisuals.SetHorizontalAngle((uid, rotVisuals), rotVisuals.DefaultRotation + (ext.InvertedCrawlingDirection ? 0 : Angle.FromDegrees(180)));
        // Have to queue an appearance update so the RotationVisualizerSystem can play an animation if the entity is already laying
        Dirty(uid, appearance);
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
