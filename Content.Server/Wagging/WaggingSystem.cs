using Content.Server.Actions;
using Content.Shared.Body;
using Content.Server.Humanoid;
using Content.Shared._DV.Humanoid;
using Content.Shared._Floof.Humanoid;
using Content.Shared.Cloning.Events;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Content.Shared.Wagging;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnWaggingMapInit);
        SubscribeLocalEvent<WaggingComponent, AppearanceLoadedEvent>(OnWaggingMapInit); // Floofstation - listen on profile load as well as map init
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
    private void OnWaggingMapInit<T>(Entity<WaggingComponent> ent, ref T args)
    {
        // Floofstation - this event can run before CompInit, at which point AddAction would throw an exception.
        if (!Initialized(ent))
            return;

        // Floofstation - remove the old action and don't add the action if the entity can't wag
        _actions.RemoveAction(ent.Comp.ActionEntity);
        if (!CanWag(ent))
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
    }

    private void OnWaggingShutdown(Entity<WaggingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnWaggingToggle(Entity<WaggingComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleWagging(ent.AsNullable());
    }

    private void OnMobStateChanged(Entity<WaggingComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Wagging)
            TryToggleWagging(ent.AsNullable());
    }

    private bool TryToggleWagging(Entity<WaggingComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!_visualBody.TryGatherMarkingsData(ent.Owner,
                [ent.Comp.Layer],
                out _,
                out _,
                out var applied))
        {
            return false;
        }

        if (!applied.TryGetValue(ent.Comp.Organ, out var markingsSet))
            return false;

        ent.Comp.Wagging = !ent.Comp.Wagging;

        markingsSet = markingsSet.ShallowClone();
        foreach (var (layers, markings) in markingsSet)
        {
            markingsSet[layers] = markingsSet[layers].ShallowClone();
            var layerMarkings = markingsSet[layers];

            for (int i = 0; i < layerMarkings.Count; i++)
            {
                var currentMarkingId = layerMarkings[i].MarkingId;
                // Floofstation - moved into a method
                if (!TryGetNewMarkingId(ent!, currentMarkingId, out var newMarkingId))
                    continue;

                layerMarkings[i] = new Marking(newMarkingId, layerMarkings[i].MarkingColors);
            }
        }

        _visualBody.ApplyMarkings(ent, new()
        {
            [ent.Comp.Organ] = markingsSet
        });
        return true;
    }

    // Floofstation section - extracted from TryToggleWagging
    public bool TryGetNewMarkingId(
        Entity<WaggingComponent> ent,
        ProtoId<MarkingPrototype> currentMarkingId,
        out ProtoId<MarkingPrototype> newMarkingId,
        bool silent = false,
        bool? isWagging = null)
    {
        isWagging ??= ent.Comp.Wagging;
        newMarkingId = string.Empty;

        if (isWagging.Value)
        {
            newMarkingId = $"{currentMarkingId}{ent.Comp.Suffix}";
        }
        else
        {
            if (currentMarkingId.Id.EndsWith(ent.Comp.Suffix))
            {
                newMarkingId = currentMarkingId.Id[..^ent.Comp.Suffix.Length];
            }
            else
            {
                newMarkingId = currentMarkingId;
                Log.Warning($"Unable to revert wagging for {currentMarkingId}");
            }
        }

        if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
        {
            Log.Warning($"{ToPrettyString(ent):ent} tried toggling wagging but {newMarkingId} marking doesn't exist");
            return false;
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
        if (!TryComp<VisualBodyComponent>(ent, out var visBodyComp)
            || !_visualBody.TryGatherMarkingsData((ent.Owner, visBodyComp), [ent.Comp.Layer], out var _, out var _, out var applied))
            return false;

        if (!applied.TryGetValue(ent.Comp.Organ, out var markingsSet))
            return false;

        // Check if any tail marking can be toggled on or off
        foreach (var (layers, markings) in markingsSet)
        {
            var layerMarkings = markingsSet[layers];
            for (int i = 0; i < layerMarkings.Count; i++)
            {
                if (TryGetNewMarkingId(ent, layerMarkings[i].MarkingId, out _, true, isWagging: false)
                    || TryGetNewMarkingId(ent, layerMarkings[i].MarkingId, out _, true, isWagging: true))
                    return true;
            }
        }

        return false;
    }
    // Floofstation section end
}
