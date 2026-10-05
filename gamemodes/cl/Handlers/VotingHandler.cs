namespace GameModes.Client;

public static class VotingHandler
{
    public static void Register()
    {
        re.Ui.OnMessage(
            types.voting.MessageNames.VotingCast,
            (types.voting.VoteCastPayload data) =>
            {
                re.Events.EmitNet(types.voting.MessageNames.VotingCast, data);
            }
        );

        re.Events.OnNet<types.voting.VoteStartedPayload>(
            types.voting.MessageNames.VotingStart,
            (payload) =>
            {
                Console.WriteLine(
                    $"[Voting] Vote started: {payload.VoteId} with options: {string.Join(", ", payload.GameModes.Select(gm => gm.Name))}"
                );
                re.Ui.SendMessage(
                    new UIMessage<types.voting.VoteStartedPayload>(
                        types.voting.MessageNames.VotingStart,
                        payload
                    )
                );
                re.Ui.SetInputPolicy(true, true, true);
            }
        );

        re.Events.OnNet<types.voting.VoteCompletedPayload>(
            types.voting.MessageNames.VotingCompleted,
            (payload) =>
            {
                Console.WriteLine(
                    $"[Voting] Vote completed: {payload.VoteId}. Winner: {payload.WinningGameModeId}"
                );
                re.Ui.SendMessage(
                    new UIMessage<types.voting.VoteCompletedPayload>(
                        types.voting.MessageNames.VotingCompleted,
                        payload
                    )
                );
                re.Ui.SetInputPolicy(false, false, false);
            }
        );
    }
}
