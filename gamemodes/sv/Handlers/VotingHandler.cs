namespace GameModes.Server;

public static class VotingHandler
{
    public static void Register()
    {
        re.Events.OnNet<types.voting.VoteCastPayload>(
            types.voting.MessageNames.VotingCast,
            (clientId, payload) =>
            {
                GameModeManager.Voting.CastVote(clientId, payload.VoteId, payload.GameModeId);
            }
        );
    }
}
