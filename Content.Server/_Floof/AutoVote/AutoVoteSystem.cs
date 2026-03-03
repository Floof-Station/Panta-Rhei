using Robust.Shared.Configuration;
using Content.Server.Voting.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Voting;
using Content.Shared._Floof.CCVar;
using Robust.Server.Player;
using Content.Server.GameTicking;

namespace Content.Server._Floof.AutoVote;

//Originaly from Einstien Engines, see the following pr:
//https://github.com/Simple-Station/Einstein-Engines/pull/1213
public sealed class AutoVoteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] public readonly IVoteManager _voteManager = default!;
    [Dependency] public readonly IPlayerManager _playerManager = default!;

    public bool _shouldVoteNextJoin = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnReturnedToLobby);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
    }

    public void OnReturnedToLobby(RoundRestartCleanupEvent ev) => CallAutovote();

    public void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        if (!_shouldVoteNextJoin)
            return;

        CallAutovote();
        _shouldVoteNextJoin = false;
    }

    private void CallAutovote()
    {
        //if we are in debug we do not want to run the auto call
        #if DEBUG
        return;
        #else
        if (!_cfg.GetCVar(FloofCCVars.AutoVoteEnabled))
            return;

        if (_playerManager.PlayerCount == 0)
        {
            _shouldVoteNextJoin = true;
            return;
        }

        if (_cfg.GetCVar(FloofCCVars.MapAutoVoteEnabled))
            _voteManager.CreateStandardVote(null, StandardVoteType.Map);
        if (_cfg.GetCVar(FloofCCVars.PresetAutoVoteEnabled))
            _voteManager.CreateStandardVote(null, StandardVoteType.Preset);
        #endif
    }
}
