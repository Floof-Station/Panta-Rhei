using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Content.Shared.Wagging;
using Robust.Shared.Prototypes;

namespace Content.Server.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WaggingComponent, ProfileLoadFinishedEvent>(OnMapInit); // Floofstation - listen on profile load as well as map init
        SubscribeLocalEvent<WaggingComponent, ComponentShutdown>(OnWaggingShutdown);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnWaggingToggle);
        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WaggingComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        EnsureComp<WaggingComponent>(args.CloneUid);
    }

    // Floofstation - listen on both profile load and map init
    private void OnMapInit(EntityUid uid, WaggingComponent component, object args)
    {
        // Floofstation - this event can run before CompInit, at which point AddAction would throw an exception.
        if (!Initialized(uid))
            return;

        // Floofstation - remove the old action and don't add the action if the entity can't wag
        _actions.RemoveAction(uid, component.ActionEntity);
        if (!CanWag((uid, component)))
            return;

        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    private void OnWaggingShutdown(EntityUid uid, WaggingComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnWaggingToggle(EntityUid uid, WaggingComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleWagging(uid, wagging: component);
    }

    private void OnMobStateChanged(EntityUid uid, WaggingComponent component, MobStateChangedEvent args)
    {
        if (component.Wagging)
            TryToggleWagging(uid, wagging: component);
    }

    public bool TryToggleWagging(EntityUid uid, WaggingComponent? wagging = null, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref wagging, ref humanoid))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            return false;

        if (markings.Count == 0)
            return false;

        wagging.Wagging = !wagging.Wagging;

        for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails
        {
            var currentMarkingId = markings[idx].MarkingId;
            // Floofstation - moved into a method
            if (!TryGetNewMarkingId((uid, wagging), currentMarkingId, out var newMarkingId))
                continue;

            _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, idx, newMarkingId,
                humanoid: humanoid);
        }

        // Floofstation - set action state
        _actions.SetToggled(wagging.ActionEntity, wagging.Wagging);

        return true;
    }

    // Floofstation section - extracted from TryToggleWagging
    public bool TryGetNewMarkingId(Entity<WaggingComponent> ent, string currentMarkingId, out string newMarkingId, bool silent = false, bool? isWagging = null)
    {
        isWagging ??= ent.Comp.Wagging;
        newMarkingId = string.Empty;

        if (isWagging.Value)
        {
            newMarkingId = $"{currentMarkingId}{ent.Comp.Suffix}";
        }
        else
        {
            if (currentMarkingId.EndsWith(ent.Comp.Suffix))
            {
                newMarkingId = currentMarkingId[..^ent.Comp.Suffix.Length];
            }
            else
            {
                newMarkingId = currentMarkingId;
                if (!silent)
                    Log.Warning($"Unable to revert wagging for {currentMarkingId}");
                return false;
            }
        }

        if (_prototype.HasIndex<MarkingPrototype>(newMarkingId))
            return true;

        if (!silent)
            Log.Warning($"{ToPrettyString(ent)} tried toggling wagging but {newMarkingId} marking doesn't exist");
        return false;

    }

    // Checks if the entity can wag
    public bool CanWag(Entity<WaggingComponent> ent)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) || markings.Count == 0)
            return false;

        // Check if any tail marking can be toggled on or off
        for (var idx = 0; idx < markings.Count; idx++)
        {
            if (TryGetNewMarkingId(ent, markings[idx].MarkingId, out _, true, isWagging: false)
                || TryGetNewMarkingId(ent, markings[idx].MarkingId, out _, true, isWagging: true))
                return true;
        }

        return false;
    }
    // Floofstation section end
}
