namespace GameModes.Server;

public static class CommandHandler
{
    public static void Register()
    {
        re.Con.RegisterCommand(
            "ban",
            args =>
            {
                var clientId =
                    args.Length > 0 && ushort.TryParse(args[0], out var id)
                        ? new re.ClientId(id)
                        : new re.ClientId();
                var banReason = args.Length > 1 ? args[1] : null;

                SessionManager.BanPlayer(clientId, banReason);
            }
        );

        re.Con.RegisterCommand(
            "testvote",
            args =>
            {
                var durationMs = args.Length > 0 && long.TryParse(args[0], out var ms) ? ms : 30000;
                GameModeManager.StartVoteForRegisteredModes(
                    durationMs,
                    new types.voting.VotingMetadata(
                        Title: "Choose Next Game Mode",
                        Description: "Vote for the next game mode!"
                    )
                );
            }
        );

        re.Con.RegisterCommand(
            "endvote",
            args =>
            {
                GameModeManager.Voting.EndVote();
            }
        );

        re.Con.RegisterCommand(
            "setmode",
            args =>
            {
                var modeId = args.FirstOrDefault();
                if (string.IsNullOrEmpty(modeId))
                {
                    Console.WriteLine("Usage: setmode <mode_id>");
                    return;
                }

                GameModeManager.ActivateGameMode(modeId);
            }
        );

        re.Con.RegisterCommand(
            "listmodes",
            args =>
            {
                var modes = GameModeManager.GetAllModeOptions();
                Console.WriteLine($"Registered game modes ({modes.Length}):");
                foreach (var mode in modes)
                {
                    Console.WriteLine($"  - {mode.Id}: {mode.Name}");
                    if (!string.IsNullOrEmpty(mode.Description))
                        Console.WriteLine($"    {mode.Description}");
                }
            }
        );

        re.Con.RegisterCommand(
            "voteresults",
            args =>
            {
                var results = GameModeManager.Voting.GetCurrentResults();
                if (results == null)
                {
                    Console.WriteLine("No active vote");
                    return;
                }

                Console.WriteLine("Current vote results:");
                foreach (var (modeId, votes) in results.OrderByDescending(kv => kv.Value))
                    Console.WriteLine($"  {modeId}: {votes} votes");
            }
        );
    }
}
