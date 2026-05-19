using Content.Shared._Euphoria.MagicalCommand;
using Content.Shared._Euphoria.Tools.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Euphoria.Tools.Systems;

public sealed class CorruptibleSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

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
        if (!TryComp<ToolComponent>(args.Used, out var tool) || !_tools.HasQuality(args.Used, requiredQuality, tool)
            || ent.Comp.RequiresEmpowered && !HasComp<EmpoweredFriendshipComponent>(args.Used))
            return;

        if (_inventory.InSlotWithFlags(ent.Owner, SlotFlags.OUTERCLOTHING))
        {
            _popupSystem.PopupClient(Loc.GetString("corruptible-component-corruption-no-start", ("target", ent.Owner)), ent, args.User);
            return;
        }

        var popup = ent.Comp.Corrupted
            ? Loc.GetString("corruptible-component-decorruption-start", ("used", args.Used))
            : Loc.GetString("corruptible-component-corruption-start", ("target", ent.Owner));
        _popupSystem.PopupPredicted(popup, ent, args.User);
        ent.Comp.CorruptStream ??= _audio.PlayPredicted(ent.Comp.CorruptStartSound, ent.Owner, args.User)?.Entity;
        _tools.UseTool(args.Used, args.User, ent.Owner, ent.Comp.Time, requiredQuality, new CorruptionFinishedEvent());
    }

    private void OnCorruptionFinished(Entity<CorruptibleComponent> ent, ref CorruptionFinishedEvent args)
    {
        ent.Comp.CorruptStream = _audio.Stop(ent.Comp.CorruptStream);
        if (args.Cancelled)
            return;

        var spawnedEnt = PredictedSpawnNextToOrDrop(ent.Comp.EntityId, ent.Owner);
        var popup = ent.Comp.Corrupted
            ? Loc.GetString("corruptible-component-decorruption-finish", ("target", ent.Owner))
            : Loc.GetString("corruptible-component-corruption-finish", ("target", ent.Owner));
        _popupSystem.PopupPredicted(popup, spawnedEnt, args.User);
        if (args.Used != null)
            // Would be nicer if I could set it to the created entity, but that only plays for a split second.
            _audio.PlayPredicted(ent.Comp.CorruptFinishSound, args.Used.Value, args.User);
        var ev = new PostCorruptingEvent
        {
            InitialItem = ent.Owner,
            CreatedItem = spawnedEnt,
        };
        RaiseLocalEvent(ent.Owner, ev);
        // Attunement passing here
        // Should probably make an event so whatever other components can pass their stuff along
        PredictedQueueDel(ent.Owner);
    }
}

[Serializable, NetSerializable]
public sealed partial class CorruptionFinishedEvent : SimpleDoAfterEvent
{
}

public sealed partial class PostCorruptingEvent : EntityEventArgs
{
    public EntityUid InitialItem;
    public EntityUid CreatedItem;
}
