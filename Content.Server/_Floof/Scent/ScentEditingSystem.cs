using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared._Coyote.SniffAndSmell;
using Content.Shared._Floof.Scent;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using ScentData = Content.Shared._Coyote.SniffAndSmell.Scent;

namespace Content.Server._Floof.Scent;

/// <summary>
///     Handles requests to edit the scents of an entity.
/// </summary>
public sealed class ScentEditingSystem : SharedScentEditingSystem
{
    [Dependency] private readonly ScentSystem _scents = default!;
    [Dependency] private readonly IAdminLogManager _logs = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<EditScentsRequestEvent>(OnEditRequest);
    }

    private void OnEditRequest(EditScentsRequestEvent msg, EntitySessionEventArgs args)
    {
        if (GetEntity(msg.TargetEntity) is not { Valid: true } targetEntity)
            return;

        if (!CanEditScent(args.SenderSession, targetEntity) || IsClientSide(targetEntity))
            return;

        var scents = SanitizeScentList(msg.Scents);
        if (_scents.TrySetScents(targetEntity, scents, true))
            _logs.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.SenderSession.AttachedEntity):player} changed the scents of {ToPrettyString(targetEntity):target}.");
    }
}
