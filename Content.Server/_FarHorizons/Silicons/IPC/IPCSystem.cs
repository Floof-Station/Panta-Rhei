using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.EUI;
using Content.Server.Hands.Systems;
using Content.Server.Ninja.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared._FarHorizons.Silicons.IPC;
using Content.Shared.Alert;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._FarHorizons.Silicons.IPC;

/// <inheritdoc/>
public sealed partial class IPCSystem : SharedIPCSystem 
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly BatteryDrainerSystem _drainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly MobStateSystem _state = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
    }

    private void AddVerbs(GetVerbsEvent<Verb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract)
            return;

        AddBrainVerbs(ev);
        AddReviveVerbs(ev);
        AddRadioVerbs(ev);
    }
}