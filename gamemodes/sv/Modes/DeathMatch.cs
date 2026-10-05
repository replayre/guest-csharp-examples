namespace GameModes.Server.Modes;

public enum DeathMatchFlavor
{
    FFA,
    TDM,
    GunGame,
}

public class DeathMatch(DeathMatchFlavor flavor) : IGameMode
{
    public enum MatchState
    {
        Uninitialized,
        Starting,
        Active,
        Ending,
    }

    public enum PlayerLifeState
    {
        Uninitialized,
        SpawnInProgress,
        Spawned,
        DeathInProgress,
        Dead,
    }

    public enum PlayerTeam
    {
        None,
        Blue,
        Red,
    }

    public class PlayerState
    {
        public PlayerLifeState LifeState;

        public bool ReadyForMatch;

        public bool JoinedDelayed;

        public double TimeOfDeath;

        public double LastTimeInArena;

        public double LastViolationEventSent;

        public PlayerTeam Team;
        public int Score;

        public string Name = "";

        public PlayerState() { }

        public PlayerState(PlayerTeam team, string name, bool joinedDelayed)
        {
            Team = team;
            Name = name;
            JoinedDelayed = joinedDelayed;
            LastTimeInArena = TimingHelper.GetTimeSeconds();
        }
    }

    private types.arena.ArenaDefinition? _currentArena = null;

    private SpawnManager? _spawnManager;

    private Dictionary<re.ClientId, PlayerState> _playerClientIds = new();

    private MatchState _matchState;

    private List<types.dm.LeaderboardEntry> _leaderboard = new();

    private double _leaderboardLastUpdate = 0.0;

    private double _matchStartTime = 0.0;

    private double _matchLastSyncTime = 0.0;

    private double _matchResultTime = 0.0;

    private const double timeToPrepareForMatch = 8.0;

    private const double timeoutToPrepareForMatch = 10.0;

    private const double timeDurationOfMatch = timeoutToPrepareForMatch + (60.0 * 10);

    private const double timeDurationOfCountdown = 5.0;

    private const double timeThresholdViolationStart = 1.0;
    private const double timeThresholdViolationFatal = 5.0;

    private const double timeRespawnDelay = 3.0;

    private const double timePostMatchScoreboard = 5.0;

    public void OnActivate()
    {
        Console.WriteLine("[DM] OnActivate");

        ArenaManager.LoadArenas();

        var arenas = ArenaManager.GetAllArenas();
        int randomChoice = Random.Shared.Next(0, arenas.Length);
        Console.WriteLine($"[DM] Chose arena {randomChoice} out of {arenas.Length}");

        _currentArena = arenas[randomChoice];
        if (_currentArena == null)
        {
            Console.WriteLine($"[DM] Failed to select arena with choice {randomChoice}");
            return;
        }

        _playerClientIds = re
            .GameClientRegistry.GetClients()
            .ToDictionary(id => id, state => new PlayerState());

        foreach (var entry in _playerClientIds)
        {
            if (flavor == DeathMatchFlavor.FFA)
            {
                entry.Value.Team = PlayerTeam.None;
            }
            else
            {
                Console.WriteLine("[DM] Implement team assignments here");
            }

            entry.Value.Name = re.cl.IdentityComponent.GetUsername(entry.Key);
        }

        _spawnManager = new SpawnManager(_currentArena.Spawnpoints.ToList());

        Console.WriteLine($"[DM] OnActivate done with {_playerClientIds.Count} clients");
    }

    public void OnDeactivate() { }

    public bool OnTick()
    {
        if (_spawnManager == null || _currentArena == null)
        {
            return false;
        }

        if (_playerClientIds.Count == 0)
        {
            return true;
        }

        _spawnManager.Tick(_playerClientIds.Keys.ToList());

        var timeNow = TimingHelper.GetTimeSeconds();

        if (_matchState == MatchState.Uninitialized)
        {
            foreach (var entry in _playerClientIds)
            {
                re.EntityId controlledEntity = re.cl.ObserverComponent.GetPrimaryObserverEntity(
                    entry.Key
                );
                if (!controlledEntity.IsValid())
                {
                    re.GameClientRegistry.DisconnectClient(entry.Key, "[DM] Error 1");
                    Console.WriteLine($"[DM] Player {entry.Key}has no entity, kick");
                    continue;
                }

                var spawnPoint = _spawnManager.GetSpawnPoint();

                re.Events.EmitNetTargeted(
                    types.dm.MessageNames.MatchPrepare,
                    entry.Key,
                    spawnPoint
                );
            }

            _matchState = MatchState.Starting;
            _matchStartTime = timeNow;
        }
        else if (_matchState == MatchState.Starting)
        {
            foreach (var entry in _playerClientIds)
            {
                re.EntityId controlledEntity = re.cl.ObserverComponent.GetPrimaryObserverEntity(
                    entry.Key
                );
                if (!controlledEntity.IsValid())
                {
                    re.GameClientRegistry.DisconnectClient(entry.Key, "[DM] Error 2");
                    Console.WriteLine($"[DM] Player {entry.Key}has no entity, kick");
                    continue;
                }

                if (
                    (timeNow - _matchStartTime) > timeToPrepareForMatch
                    && !entry.Value.ReadyForMatch
                )
                {
                    re.GameClientRegistry.DisconnectClient(entry.Key, "[DM] Error 3");
                    Console.WriteLine($"[DM] Player {entry.Key} still not ready, kick");
                    continue;
                }
            }

            if ((timeNow - _matchStartTime) > timeoutToPrepareForMatch)
            {
                re.Events.EmitNetTargeted(
                    types.dm.MessageNames.MatchBegin,
                    _playerClientIds.Keys.ToList(),
                    new types.dm.MatchBeginPayload(
                        timeDurationOfCountdown,
                        timeNow - _matchStartTime
                    )
                );
                _matchState = MatchState.Active;
                Console.WriteLine($"[DM] Match is now active");
            }
        }
        else if (_matchState == MatchState.Active)
        {
            if ((timeNow - _matchStartTime) > timeDurationOfMatch)
            {
                Console.WriteLine($"[DM] Match is now ending");
                _matchState = MatchState.Ending;
                return false;
            }

            foreach (var entry in _playerClientIds)
            {
                re.EntityId controlledEntity = re.cl.ObserverComponent.GetPrimaryObserverEntity(
                    entry.Key
                );
                if (
                    !controlledEntity.IsValid()
                    || !re.ecs.TransformComponent.Exists(controlledEntity)
                )
                {
                    continue;
                }

                Vector3 playerPos = re.ecs.TransformComponent.GetPosition(controlledEntity);

                if (entry.Value.LifeState == PlayerLifeState.Spawned)
                {
                    float distSq = (playerPos - _currentArena.center).LengthSquared();
                    if (distSq < _currentArena.range)
                    {
                        entry.Value.LastTimeInArena = timeNow;

                        if (entry.Value.LastViolationEventSent > 0.0)
                        {
                            re.Events.EmitNetTargeted(
                                types.dm.MessageNames.ArenaBorderViolation,
                                entry.Key,
                                new types.dm.ArenaBorderViolation(0)
                            );
                            entry.Value.LastViolationEventSent = 0.0;
                        }
                    }
                    else
                    {
                        var violationTimer = timeNow - entry.Value.LastTimeInArena;
                        if (
                            violationTimer > timeThresholdViolationStart
                            && violationTimer <= timeThresholdViolationFatal
                            && (timeNow - entry.Value.LastViolationEventSent) > 1.0
                        )
                        {
                            re.Events.EmitNetTargeted(
                                types.dm.MessageNames.ArenaBorderViolation,
                                entry.Key,
                                new types.dm.ArenaBorderViolation(
                                    timeThresholdViolationFatal - violationTimer
                                )
                            );

                            entry.Value.LastViolationEventSent = timeNow;
                            Console.WriteLine(
                                $"[DM] Player {entry.Key} got outside the Arena. Sending warning!"
                            );
                        }
                        else if (violationTimer > timeThresholdViolationFatal)
                        {
                            re.Events.EmitNetTargeted(types.dm.MessageNames.ForceKill, entry.Key);
                            entry.Value.LifeState = PlayerLifeState.DeathInProgress;
                            Console.WriteLine(
                                $"[DM] Player {entry.Key} was outside the Arena for too long, killing!"
                            );
                        }
                    }
                }
                else if (entry.Value.LifeState == PlayerLifeState.Dead)
                {
                    var respawnTimer = timeNow - entry.Value.TimeOfDeath;
                    if (respawnTimer > timeRespawnDelay)
                    {
                        var spawnPoint = _spawnManager.GetSpawnPoint();

                        re.Events.EmitNetTargeted(
                            types.dm.MessageNames.RespawnPlayer,
                            entry.Key,
                            spawnPoint
                        );
                        entry.Value.LifeState = PlayerLifeState.SpawnInProgress;
                    }
                }
            }

            if ((timeNow - _matchLastSyncTime) > 1.0)
            {
                if ((timeNow - _leaderboardLastUpdate) > 5.0)
                {
                    _leaderboard.Clear();

                    if (flavor == DeathMatchFlavor.FFA || flavor == DeathMatchFlavor.GunGame)
                    {
                        foreach (var entry in _playerClientIds)
                        {
                            _leaderboard.Add(
                                new types.dm.LeaderboardEntry(entry.Value.Name, entry.Value.Score)
                            );
                        }
                    }
                    else
                    {
                        int scoreTeamBlue = 0;
                        int scoreTeamRed = 0;
                        foreach (var entry in _playerClientIds)
                        {
                            if (entry.Value.Team == PlayerTeam.Blue)
                            {
                                scoreTeamBlue += entry.Value.Score;
                            }
                            else if (entry.Value.Team == PlayerTeam.Red)
                            {
                                scoreTeamRed += entry.Value.Score;
                            }
                            else
                            {
                                Console.WriteLine(
                                    $"[DM] Player {entry.Key} has no team assigned in team mode!"
                                );
                            }
                        }

                        _leaderboard.Add(new types.dm.LeaderboardEntry("Team Blue", scoreTeamBlue));
                        _leaderboard.Add(new types.dm.LeaderboardEntry("Team Red", scoreTeamRed));
                    }

                    _leaderboard = _leaderboard.OrderByDescending(x => x.Score).ToList();

                    _leaderboardLastUpdate = timeNow;
                }

                re.Events.EmitNetTargeted(
                    types.dm.MessageNames.MatchSync,
                    _playerClientIds.Keys.ToList(),
                    new types.dm.MatchSyncPayload(
                        timeDurationOfMatch - (timeNow - _matchStartTime),
                        _leaderboard.ToArray()
                    )
                );

                _matchLastSyncTime = timeNow;
            }
        }
        else if (_matchState == MatchState.Ending)
        {
            if (_matchResultTime == 0.0)
            {
                Console.WriteLine($"[DM] Displaying scoreboard");

                re.Events.EmitNetTargeted(
                    types.dm.MessageNames.MatchEnding,
                    _playerClientIds.Keys.ToList()
                );
                _matchResultTime = timeNow;
            }

            if ((timeNow - _matchResultTime) > timePostMatchScoreboard)
            {
                Console.WriteLine($"[DM] Ending gamemode");
                return true;
            }
        }

        return false;
    }

    public void OnClientDelayedJoin(re.ClientId client)
    {
        Console.WriteLine($"[DM] OnClientDelayedJoin {client}");

        if (_spawnManager == null)
        {
            return;
        }

        _playerClientIds.Add(
            client,
            new PlayerState(PlayerTeam.None, re.cl.IdentityComponent.GetUsername(client), true)
        );

        var spawnPoint = _spawnManager.GetSpawnPoint();

        re.Events.EmitNetTargeted(types.dm.MessageNames.MatchPrepare, client, spawnPoint);
    }

    public void OnClientDisconnect(re.ClientId client)
    {
        Console.WriteLine($"[DM] OnClientDisconnect {client}");

        _playerClientIds.Remove(client);
    }

    public double GetRemainingRuntime()
    {
        return timeDurationOfMatch - (TimingHelper.GetTimeSeconds() - _matchStartTime);
    }

    public int GetNumberOfPlayers()
    {
        return _playerClientIds.Count;
    }

    public bool IsDelayedJoiningAllowed()
    {
        return _matchState == MatchState.Active;
    }

    public void OnClientReadyForMatch(re.ClientId client)
    {
        Console.WriteLine($"[DM] OnClientReadyForMatch {client}");

        _playerClientIds.TryGetValue(client, out var playerState);
        if (playerState != null)
        {
            playerState.ReadyForMatch = true;
            playerState.LifeState = PlayerLifeState.Spawned;

            if (playerState.JoinedDelayed)
            {
                var timeNow = TimingHelper.GetTimeSeconds();

                re.Events.EmitNetTargeted(
                    types.dm.MessageNames.MatchBegin,
                    client,
                    new types.dm.MatchBeginPayload(
                        timeDurationOfCountdown,
                        timeNow - _matchStartTime
                    )
                );

                Console.WriteLine($"[DM] sent match begin to {client}");
            }
        }
    }

    public void OnClientRespawned(re.ClientId client)
    {
        Console.WriteLine($"[DM] OnClientRespawned {client}");

        _playerClientIds.TryGetValue(client, out var playerState);
        if (playerState != null)
        {
            var timeNow = TimingHelper.GetTimeSeconds();

            playerState.LifeState = PlayerLifeState.Spawned;
            playerState.TimeOfDeath = 0.0;
            playerState.LastTimeInArena = timeNow;
        }
    }

    public void OnPlayerKilled(re.ClientId client, types.dm.PlayerKilledPayload payload)
    {
        var killerOwner = new re.ClientId();
        foreach (var entry in _playerClientIds)
        {
            if (re.cl.ObserverComponent.GetPrimaryObserverEntity(entry.Key) == payload.killer)
            {
                killerOwner = entry.Key;
                break;
            }
        }

        if (
            killerOwner.IsValid()
            && _playerClientIds.TryGetValue(killerOwner, out var playerState)
            && client != killerOwner
        )
        {
            re.Events.EmitNetTargeted(types.dm.MessageNames.KillConfirmed, killerOwner);
            playerState.Score++;

            _leaderboardLastUpdate = 0.0;
        }
    }

    public void OnClientDeath(re.ClientId client)
    {
        Console.WriteLine($"[DM] OnClientDeath {client}");

        _playerClientIds.TryGetValue(client, out var playerState);
        if (playerState != null)
        {
            playerState.LifeState = PlayerLifeState.Dead;
            playerState.TimeOfDeath = TimingHelper.GetTimeSeconds();
        }
    }
}
