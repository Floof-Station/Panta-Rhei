using Content.Shared._Euphoria.Tools.Components;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Euphoria.Tools.Systems;

public sealed class CorruptibleSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly ProtoId<ToolQualityPrototype> CorruptingQuality = "Corrupting";
    private static readonly ProtoId<ToolQualityPrototype> DecorruptingQuality = "Decorrupting";

    public override void Initialize()
    {
        SubscribeLocalEvent<CorruptibleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CorruptibleComponent, CorruptionFinishedEvent>(OnCorruptionFinished);
    }

    private void OnInteractUsing(Entity<CorruptibleComponent> ent, ref InteractUsingEvent args)
    {
        var requiredQuality = ent.Comp.Corrupted ? DecorruptingQuality : CorruptingQuality;
        if (!TryComp<ToolComponent>(args.Used, out var tool) || !_tools.HasQuality(args.Used, requiredQuality, tool))
            return;

        ent.Comp.CorruptStream ??= _audio.PlayPredicted(ent.Comp.CorruptStartSound, ent.Owner, args.User)?.Entity;
        _tools.UseTool(args.Used, args.User, ent.Owner, ent.Comp.Time, requiredQuality, new CorruptionFinishedEvent());
    }

    private void OnCorruptionFinished(Entity<CorruptibleComponent> ent, ref CorruptionFinishedEvent args)
    {
        ent.Comp.CorruptStream = _audio.Stop(ent.Comp.CorruptStream);
        if (args.Cancelled)
            return;

        var spawnedEnt = PredictedSpawnNextToOrDrop(ent.Comp.EntityId, ent.Owner);
        if (args.Used != null)
            // Would be nicer if I could set it to the created entity, but that only plays for a split second.
            _audio.PlayPredicted(ent.Comp.CorruptFinishSound, args.Used.Value, args.User);
        // Attunement passing here
        // also add a popup
        PredictedQueueDel(ent.Owner);
    }
}

[Serializable, NetSerializable]
public sealed partial class CorruptionFinishedEvent : SimpleDoAfterEvent
{
}
