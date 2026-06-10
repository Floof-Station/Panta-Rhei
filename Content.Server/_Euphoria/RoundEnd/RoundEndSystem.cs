using Content.Server.Voting.Managers;
using Content.Server.Voting;
using Content.Shared._DV.CCVars;

namespace Content.Server.RoundEnd;

public sealed partial class RoundEndSystem : EntitySystem
{
    public void CallEvacuationSecretBallot()
    {
        var options = new VoteOptions
        {
            Title = Loc.GetString("round-end-system-vote-title"),
            Duration = _cfg.GetCVar(DCCVars.EmergencyShuttleVoteTime),
            DisplayVotes = false,
            InitiatorText = Loc.GetString("vote-options-server-initiator-text")
        };

        options.Options.Add((Loc.GetString("round-end-system-vote-end"), true));
        options.Options.Add((Loc.GetString("round-end-system-vote-continue"), false));

        var vote = _vote.CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            if (args.Winner == null || (bool)args.Winner)
                RequestRoundEnd(null, false, "round-end-system-vote-shuttle-called-announcement");
        };
    }
}