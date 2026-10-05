namespace GameModes.Server;

public static class GameModeManager
{
    public class GameModeDefinition
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool Hidden { get; init; } = false;
        public string? Description { get; init; }
        public string? Icon { get; init; }
        public Func<IGameMode?>? OnResolveGameMode { get; init; }
        public IGameMode? GameMode;
    }

    private static readonly Dictionary<string, GameModeDefinition> _registeredModes = [];
    private static GameModeDefinition? _baseMode;
    private static GameModeDefinition? _activeMode;
    private static int _tickGeneration = 0;
    private static double _lastInfoSendTime = 0.0;
    private static readonly object _modeLock = new();
    public static VotingManager Voting { get; } = new();
    public static bool DebugMode { get; set; } = false;

    public static void RegisterGameMode(GameModeDefinition mode)
    {
        lock (_modeLock)
        {
            if (_registeredModes.ContainsKey(mode.Id))
            {
                Console.WriteLine($"[GameModeManager] Game mode already registered: {mode.Id}");
                return;
            }

            _registeredModes[mode.Id] = mode;
            Console.WriteLine($"[GameModeManager] Registered game mode: {mode.Id} ({mode.Name})");
        }
    }

    public static void ActivateBaseMode(string gameModeId)
    {
        lock (_modeLock)
        {
            if (!_registeredModes.TryGetValue(gameModeId, out var mode))
            {
                Console.WriteLine($"[GameModeManager] Base mode not found: {gameModeId}");
                return;
            }

            _baseMode = mode;
            if (_baseMode.GameMode != null)
            {
                Console.WriteLine($"[GameModeManager] Base mode was expected to be null here");
                return;
            }

            mode.GameMode = mode.OnResolveGameMode?.Invoke();
            mode.GameMode?.OnActivate();
            Console.WriteLine($"[GameModeManager] Activated base mode: {gameModeId}");

            Future.Spawn(async () =>
            {
                while (true)
                {
                    _baseMode?.GameMode?.OnTick();
                    await Future.Yield();
                }
            });
        }
    }

    public static void ActivateGameMode(string gameModeId)
    {
        lock (_modeLock)
        {
            if (!_registeredModes.TryGetValue(gameModeId, out var mode))
            {
                Console.WriteLine($"[GameModeManager] Game mode not found: {gameModeId}");
                return;
            }

            if (_activeMode != null)
            {
                _activeMode.GameMode?.OnDeactivate();
                Console.WriteLine($"[GameModeManager] Deactivated game mode: {_activeMode.Id}");
            }

            _activeMode = mode;
            if (_activeMode.GameMode != null)
            {
                Console.WriteLine($"[GameModeManager] Nulling existing game mode instance");
                _activeMode.GameMode = null;
            }

            re.Events.EmitNetBroadcast(
                types.gamemodes.MessageNames.ModeActivated,
                new types.gamemodes.ModeActivated(gameModeId)
            );

            mode.GameMode = mode.OnResolveGameMode?.Invoke();
            mode.GameMode?.OnActivate();
            Console.WriteLine($"[GameModeManager] Activated game mode: {gameModeId}");

            var currentGeneration = ++_tickGeneration;
            Future.Spawn(async () =>
            {
                while (currentGeneration == _tickGeneration)
                {
                    if (_activeMode?.GameMode?.OnTick() == true)
                    {
                        EndGameMode();
                        break;
                    }

                    var timeNow = TimingHelper.GetTimeSeconds();
                    if ((timeNow - _lastInfoSendTime) >= 1.0 && _activeMode?.GameMode != null)
                    {
                        re.Events.EmitNetBroadcast(
                            types.gamemodes.MessageNames.ActiveModeUpdate,
                            new types.gamemodes.ActiveModeUpdate(
                                true,
                                _activeMode?.GameMode?.IsDelayedJoiningAllowed() ?? false,
                                _activeMode!.Name,
                                (int)(_activeMode?.GameMode?.GetRemainingRuntime() ?? 0),
                                _activeMode?.GameMode?.GetNumberOfPlayers() ?? 0
                            )
                        );

                        _lastInfoSendTime = timeNow;
                    }

                    await Future.Yield();
                }
            });
        }
    }

    public static void EndGameMode()
    {
        lock (_modeLock)
        {
            if (_activeMode != null)
            {
                _activeMode.GameMode?.OnDeactivate();
                Console.WriteLine($"[GameModeManager] Deactivated game mode: {_activeMode.Id}");
                _activeMode.GameMode = null;
                _activeMode = null;

                re.Events.EmitNetBroadcast(
                    types.gamemodes.MessageNames.ModeActivated,
                    new types.gamemodes.ModeActivated(_baseMode!.Id)
                );

                if (!DebugMode)
                {
                    Future.Spawn(async () =>
                    {
                        Console.WriteLine(
                            $"[GameModeManager] Queueing vote in 30 seconds unless interrupted"
                        );
                        await Future.Sleep(30 * 1000);

                        if (GetActiveMode() != null || Voting.IsVoteActive())
                        {
                            Console.WriteLine(
                                $"[GameModeManager] Already in mode or a vote is active, aborting"
                            );
                            return;
                        }

                        StartVoteForRegisteredModes(
                            30 * 1000,
                            new types.voting.VotingMetadata(
                                Title: "Choose Next Game Mode",
                                Description: "Vote for the next game mode!"
                            )
                        );
                    });
                }
            }
        }
    }

    public static GameModeDefinition? GetActiveMode()
    {
        lock (_modeLock)
        {
            return _activeMode;
        }
    }

    public static GameModeDefinition? GetMode(string gameModeId)
    {
        lock (_modeLock)
        {
            return _registeredModes.TryGetValue(gameModeId, out var mode) ? mode : null;
        }
    }

    public static types.gamemodes.GameModeOption[] GetAllModeOptions()
    {
        lock (_modeLock)
        {
            return _registeredModes
                .Values.Where(m => !m.Hidden)
                .Select(m => new types.gamemodes.GameModeOption(
                    m.Id,
                    m.Name,
                    m.Hidden,
                    m.Description,
                    m.Icon
                ))
                .ToArray();
        }
    }

    public static void OnDeactivate()
    {
        _baseMode?.GameMode?.OnDeactivate();
        _activeMode?.GameMode?.OnDeactivate();
    }

    public static void OnClientDisconnect(re.ClientId client)
    {
        _baseMode?.GameMode?.OnClientDisconnect(client);
        _activeMode?.GameMode?.OnClientDisconnect(client);
    }

    public static void OnClientRequestDelayedJoin(re.ClientId client)
    {
        if (_activeMode?.GameMode?.IsDelayedJoiningAllowed() == true)
        {
            re.Events.EmitNetTargeted(
                types.gamemodes.MessageNames.ModeActivated,
                client,
                new types.gamemodes.ModeActivated(_activeMode.Id)
            );

            _activeMode?.GameMode?.OnClientDelayedJoin(client);
        }
    }

    public static void OnClientReady(re.ClientId client)
    {
        Console.WriteLine($"[GameModeManager] Client {client} signaled ready, enabling Lobby mode");

        re.Events.EmitNetTargeted(
            types.gamemodes.MessageNames.ModeActivated,
            client,
            new types.gamemodes.ModeActivated(_baseMode!.Id)
        );

        if (!DebugMode && GetActiveMode() == null && !Voting.IsVoteActive())
        {
            Future.Spawn(async () =>
            {
                await Future.Sleep(10 * 1000);

                if (DebugMode || GetActiveMode() != null || Voting.IsVoteActive())
                {
                    return;
                }

                Console.WriteLine(
                    $"[GameModeManager] Player connected while no mode was active. Starting vote"
                );

                StartVoteForRegisteredModes(
                    30 * 1000,
                    new types.voting.VotingMetadata(
                        Title: "Choose Next Game Mode",
                        Description: "Vote for the next game mode!"
                    )
                );
            });
        }
    }

    public static void StartVote(
        types.gamemodes.GameModeOption[] options,
        long durationMs,
        types.voting.VotingMetadata? metadata = null
    )
    {
        var voteId = $"vote-{Guid.NewGuid():N}";
        Voting.StartVote(
            voteId,
            options,
            durationMs,
            metadata,
            async (string? winningGameModeId) =>
            {
                if (winningGameModeId == null)
                {
                    Console.WriteLine(
                        $"[GameModeManager] No winner in vote. Retrying in 30 seconds..."
                    );

                    if (!DebugMode)
                    {
                        Future.Spawn(async () =>
                        {
                            await Future.Sleep(3000);

                            if (GetActiveMode() != null || Voting.IsVoteActive())
                            {
                                return;
                            }

                            StartVoteForRegisteredModes(durationMs, metadata);
                        });
                    }

                    return;
                }

                Console.WriteLine($"[GameModeManager] Vote completed. Winner: {winningGameModeId}");

                if (_registeredModes.ContainsKey(winningGameModeId))
                {
                    await Future.Sleep(3000);

                    ActivateGameMode(winningGameModeId);
                }
            }
        );
    }

    public static void StartVoteForRegisteredModes(
        long durationMs,
        types.voting.VotingMetadata? metadata = null
    )
    {
        lock (_modeLock)
        {
            var options = _registeredModes
                .Values.Where(m => !m.Hidden)
                .Select(m => new types.gamemodes.GameModeOption(
                    m.Id,
                    m.Name,
                    m.Hidden,
                    m.Description,
                    m.Icon
                ))
                .ToArray();

            if (options.Length == 0)
            {
                Console.WriteLine("[GameModeManager] No game modes registered for voting");
                return;
            }

            StartVote(options, durationMs, metadata);
        }
    }
}
