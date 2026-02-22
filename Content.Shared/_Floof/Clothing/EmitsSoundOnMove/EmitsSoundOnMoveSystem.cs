using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._Floof.Clothing.EmitsSoundOnMove;

// Note: this system has been ported from FS14 by its author and modified to fit the quality standards
public sealed class EmitsSoundOnMoveSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    private EntityQuery<InputMoverComponent> _moverQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<ClothingComponent> _clothingQuery;

    public override void Initialize()
    {
        _moverQuery = GetEntityQuery<InputMoverComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _clothingQuery = GetEntityQuery<ClothingComponent>();

        SubscribeLocalEvent<EmitsSoundOnMoveComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EmitsSoundOnMoveComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, EmitsSoundOnMoveComponent component, GotEquippedEvent args)
    {
        component.IsSlotValid = !args.SlotFlags.HasFlag(SlotFlags.POCKET);
    }

    private void OnUnequipped(EntityUid uid, EmitsSoundOnMoveComponent component, GotUnequippedEvent args)
    {
        component.IsSlotValid = true;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EmitsSoundOnMoveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateSound(uid, comp);
        }
        query.Dispose();
    }

    private void UpdateSound(EntityUid uid, EmitsSoundOnMoveComponent component)
    {
        if (!_xformQuery.TryGetComponent(uid, out var xform))
            return;

        // Space does not transmit sound
        if (xform.GridUid == null || component.RequiresGravity && _gravity.IsWeightless(uid))
            return;

        var parent = Transform(uid).ParentUid;
        var isWorn = parent is { Valid: true } &&
                     _clothingQuery.TryGetComponent(uid, out var clothing)
                     && clothing.InSlot != null
                     && component.IsSlotValid;
        var user = isWorn ? parent : uid;

        // If this entity is worn by another entity, use that entity's coordinates
        var coordinates = isWorn ? Transform(parent).Coordinates : Transform(uid).Coordinates;
        var distanceNeeded = (isWorn && _moverQuery.TryGetComponent(parent, out var mover) && mover.Sprinting)
            ? 1.5f // The parent is a mob that is currently sprinting
            : 2f; // The parent is not a mob or is not sprinting

        if (!coordinates.TryDistance(EntityManager, component.LastPosition, out var distance))
            component.SoundDistance = 0;
        else
            component.SoundDistance += distance;

        component.LastPosition = coordinates;
        if (component.SoundDistance < distanceNeeded)
            return;
        // Don't accumulate more than 2 sounds in a row
        component.SoundDistance = Math.Min(component.SoundDistance - distanceNeeded, distanceNeeded * 0.9f);

        var sound = component.SoundCollection;
        var audioParams = sound.Params
            .WithVolume(sound.Params.Volume)
            .WithVariation(sound.Params.Variation ?? 0f);

        // The client only predicts the sound of its own clothing. "client" in this case is the entity wearing the clothing, aka [parent].
        // On the server side, PlayPredicted will not send the sound to the wearer of the clothing.
        if (_net.IsClient && _playerMan.LocalEntity != user)
            return;

        _audio.PlayPredicted(sound, uid, user, audioParams);
    }
}
