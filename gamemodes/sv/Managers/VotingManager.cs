public class VotingManager
{
    private class ActiveVote
    {
        public string VoteId { get; init; } = string.Empty;
        public Dictionary<string, int> Votes { get; } = new();
        public types.gamemodes.GameModeOption[] Options { get; init; } = [];
        public DateTime StartTime { get; init; }
        public long DurationMs { get; init; }
        public Dictionary<re.ClientId, string> ClientVotes { get; } = new();
        public Action<string?>? OnVoteCompleted { get; init; }
    }

    private ActiveVote? _currentVote;
    private readonly object _voteLock = new();

    public async void StartVote(
        string voteId,
        types.gamemodes.GameModeOption[] gameModes,
        long durationMs,
        types.voting.VotingMetadata? metadata = null,
        Action<string?>? onVoteCompleted = null
    )
    {
        lock (_voteLock)
        {
            if (_currentVote != null)
            {
                Console.WriteLine(
                    $"[VotingManager] Vote already in progress: {_currentVote.VoteId}"
                );
                return;
            }

            _currentVote = new ActiveVote
            {
                VoteId = voteId,
                Options = gameModes,
                StartTime = DateTime.UtcNow,
                DurationMs = durationMs,
                OnVoteCompleted = onVoteCompleted,
            };

            // Initialize vote counts
            foreach (var mode in gameModes)
            {
                _currentVote.Votes[mode.Id] = 0;
            }

            var payload = new types.voting.VoteStartedPayload(
                VoteId: voteId,
                GameModes: gameModes,
                DurationMs: durationMs,
                Metadata: metadata
            );

            re.Events.EmitNetBroadcast(types.voting.MessageNames.VotingStart, payload);
            Console.WriteLine(
                $"[VotingManager] Started vote: {voteId} with {gameModes.Length} options for {durationMs}ms"
            );
        }

        // TODO: there is a bug here. If EndVote is invoked via server ConCmd
        // this will still execute in the future and cancel a pontential next vote that was launched
        //
        await Future.Sleep((ulong)durationMs);
        EndVote();
    }

    public bool CastVote(re.ClientId clientId, string voteId, string gameModeId)
    {
        lock (_voteLock)
        {
            if (_currentVote == null)
            {
                Console.WriteLine($"[VotingManager] No active vote for client {clientId}");
                return false;
            }

            if (_currentVote.VoteId != voteId)
            {
                Console.WriteLine(
                    $"[VotingManager] Vote ID mismatch: expected {_currentVote.VoteId}, got {voteId}"
                );
                return false;
            }

            if (!_currentVote.Votes.ContainsKey(gameModeId))
            {
                Console.WriteLine($"[VotingManager] Invalid game mode: {gameModeId}");
                return false;
            }

            // Check if client has already voted
            if (_currentVote.ClientVotes.TryGetValue(clientId, out var previousVote))
            {
                if (previousVote == gameModeId)
                {
                    Console.WriteLine(
                        $"[VotingManager] Client {clientId} already voted for {gameModeId}"
                    );
                    return false;
                }

                // Remove previous vote
                _currentVote.Votes[previousVote]--;
                Console.WriteLine(
                    $"[VotingManager] Client {clientId} changed vote from {previousVote} to {gameModeId}"
                );
            }

            // Record the new vote
            _currentVote.Votes[gameModeId]++;
            _currentVote.ClientVotes[clientId] = gameModeId;

            Console.WriteLine(
                $"[VotingManager] Client {clientId} voted for {gameModeId}. Current count: {_currentVote.Votes[gameModeId]}"
            );
            return true;
        }
    }

    public void EndVote()
    {
        lock (_voteLock)
        {
            if (_currentVote == null)
            {
                return;
            }

            var winner = _currentVote.Votes.OrderByDescending(kv => kv.Value).FirstOrDefault();
            var winningGameModeId = (winner.Value > 0) ? winner.Key : null;

            re.Events.EmitNetBroadcast(
                types.voting.MessageNames.VotingCompleted,
                new types.voting.VoteCompletedPayload(
                    VoteId: _currentVote.VoteId,
                    WinningGameModeId: winningGameModeId
                )
            );

            if (winningGameModeId == null)
            {
                Console.WriteLine(
                    $"[VotingManager] Vote {_currentVote.VoteId} completed. No winner! {winningGameModeId}"
                );
            }
            else
            {
                Console.WriteLine(
                    $"[VotingManager] Vote {_currentVote.VoteId} completed. Winner: {winningGameModeId} with {winner.Value} votes"
                );
            }

            _currentVote.OnVoteCompleted?.Invoke(winningGameModeId);
            _currentVote = null;
        }
    }

    public void CancelVote()
    {
        lock (_voteLock)
        {
            if (_currentVote != null)
            {
                Console.WriteLine($"[VotingManager] Cancelled vote: {_currentVote.VoteId}");
                _currentVote = null;
            }
        }
    }

    public bool IsVoteActive()
    {
        lock (_voteLock)
        {
            return _currentVote != null;
        }
    }

    public string? GetCurrentVoteId()
    {
        lock (_voteLock)
        {
            return _currentVote?.VoteId;
        }
    }

    public Dictionary<string, int>? GetCurrentResults()
    {
        lock (_voteLock)
        {
            return _currentVote != null ? new Dictionary<string, int>(_currentVote.Votes) : null;
        }
    }
}
