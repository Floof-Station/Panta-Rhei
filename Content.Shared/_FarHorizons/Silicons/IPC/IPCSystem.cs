using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Silicons.IPC;

/// <summary>
/// This handles all logic related to IPC. The system is modular, you can pick and choose components independently from each other
/// </summary>
public abstract partial class SharedIPCSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _items = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float TimerDelay = 1f;
    private float _timer = 0f;

    public override void Initialize()
    {
        base.Initialize();

        SetupBrain();
        SetupRevive();
        SetupBattery();
        SetupRadio();
        SetupLock();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _timer += frameTime;
        if (_timer < TimerDelay)
            return;
        _timer -= TimerDelay;

        UpdateBattery(TimerDelay);
    }
}