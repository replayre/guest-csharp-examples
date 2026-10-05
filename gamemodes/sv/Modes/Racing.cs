namespace GameModes.Server.Modes;

public class Racing : IGameMode
{
    private const double timeRaceMaxLength = 60.0 * 10;
    private const double timeToPrepareForRace = 15.0;
    private const double timeToFinishRace = 30.0;
    private const double timeDurationOfCountdown = 5.0;
    private const double timePostRaceScoreboard = 10.0;

    public enum RaceState
    {
        Uninitialized,
        Starting,
        Countdown,
        Active,
        Ending,
    }

    public class RaceParticipant
    {
        public string Name = "";
        public re.EntityId VehicleId = new();
        public types.racing.GridPosition StartGridPosition;
        public int CurrentCheckpoint;
        public double LastCheckpointPassedTime;
        public bool ReadyForRace;
        public bool FinishedRace;
    }

    private Dictionary<re.ClientId, RaceParticipant> _participants = new();
    private RaceState _state;
    private types.racing.Track? _track;
    private double _raceStartTime = 0.0;
    private double _raceEndTime = 0.0;
    private double _raceCountdownStartTime = 0.0;
    private double _raceActiveStartTime = 0.0;
    private double _raceResultTime = 0.0;
    private TDBID _randomVehicle = new TDBID();

    public async void OnActivate()
    {
        Console.WriteLine($"[Racing] activated");

        RaceTrackManager.LoadTracks();

        _track = RaceTrackManager.PickRandomTrack();
        if (_track == null)
        {
            Console.WriteLine($"[Racing] No tracks available");
            return;
        }

        _randomVehicle = VehicleManager.GetRandomVehicle();

        Console.WriteLine(
            $"[Racing] Selected track: {_track.name} ({_track.id}) with random vehicle choice {_randomVehicle.Hash}"
        );
    }

    public void OnDeactivate()
    {
        foreach (var entry in _participants)
        {
            if (entry.Value.VehicleId.IsValid())
            {
                re.EntitySystem.Delete(entry.Value.VehicleId);
            }
        }
    }

    public bool OnTick()
    {
        var timeNow = TimingHelper.GetTimeSeconds();

        if (_state == RaceState.Uninitialized)
        {
            if (_track?.gridPositions is { Length: > 0 } gridPositions)
            {
                _participants = re
                    .GameClientRegistry.GetClients()
                    .OrderBy(_ => Guid.NewGuid())
                    .ToDictionary(id => id, entry => new RaceParticipant());

                if (_participants.Count > gridPositions.Length)
                {
                    Console.WriteLine(
                        $"[Racing] Track cant accomodate {_participants.Count} players. Max grid positons: {gridPositions.Length}"
                    );
                    return true;
                }

                int gridIndex = 0;
                foreach (var entry in _participants)
                {
                    entry.Value.Name = re.cl.IdentityComponent.GetUsername(entry.Key);
                    entry.Value.StartGridPosition = gridPositions[gridIndex];

                    re.Events.EmitNetTargeted(
                        types.racing.MessageNames.RaceSetup,
                        entry.Key,
                        new types.racing.RaceSetupPayload(_track, gridIndex, _participants.Count)
                    );

                    gridIndex++;
                }
            }
            else
            {
                Console.WriteLine("[Racing] No grid positions defined on track - aborting race");
                return true;
            }

            _state = RaceState.Starting;
            _raceStartTime = timeNow;
        }
        else if (_state == RaceState.Starting)
        {
            foreach (var entry in _participants)
            {
                re.EntityId controlledEntity = re.cl.ObserverComponent.GetPrimaryObserverEntity(
                    entry.Key
                );
                if (!controlledEntity.IsValid())
                {
                    re.GameClientRegistry.DisconnectClient(entry.Key, "[Racing] Error 1");
                    Console.WriteLine($"[Racing] Player {entry.Key} has no entity, kick");
                    continue;
                }

                if ((timeNow - _raceStartTime) > timeToPrepareForRace && !entry.Value.ReadyForRace)
                {
                    //re.GameClientRegistry.DisconnectClient(entry.Key, "[Racing] Error 2");
                    Console.WriteLine($"[DM] Player {entry.Key} still not ready, kick");
                    continue;
                }
            }

            if ((timeNow - _raceStartTime) > timeToPrepareForRace)
            {
                re.Events.EmitNetTargeted(
                    types.racing.MessageNames.RaceCountdown,
                    _participants.Keys.ToList()
                );
                Console.WriteLine("[Racing] Countdown started (3s)");
                _state = RaceState.Countdown;
                _raceCountdownStartTime = timeNow;
            }
        }
        else if (_state == RaceState.Countdown)
        {
            if ((timeNow - _raceCountdownStartTime) > timeDurationOfCountdown)
            {
                re.Events.EmitNetTargeted(
                    types.racing.MessageNames.RaceStart,
                    _participants.Keys.ToList()
                );
                Console.WriteLine("[Racing] Race started");
                _state = RaceState.Active;
                _raceActiveStartTime = timeNow;
            }
        }
        else if (_state == RaceState.Active)
        {
            if (_raceEndTime == 0.0 && (timeNow - _raceActiveStartTime) > timeRaceMaxLength)
            {
                Console.WriteLine($"[Racing] Match exceeded timeout");

                _raceEndTime = timeNow + timeToFinishRace;
                BroadcastPositions();
            }

            if (_raceEndTime > 0 && timeNow > _raceEndTime)
            {
                Console.WriteLine($"[Racing] Match is now ending because of winner timeout");
                _state = RaceState.Ending;
            }

            if (_participants.Count == 0)
            {
                Console.WriteLine($"[Racing] Match is now ending because everyone disconnected");
                _state = RaceState.Ending;
            }
        }
        else if (_state == RaceState.Ending)
        {
            if (_raceResultTime == 0.0)
            {
                Console.WriteLine($"[Racing] Displaying scoreboard");

                BroadcastPositions();
                _raceResultTime = timeNow;
            }

            if ((timeNow - _raceResultTime) > timePostRaceScoreboard)
            {
                Console.WriteLine($"[Racing] Ending gamemode");
                return true;
            }
        }

        return false;
    }

    public void OnClientDisconnect(re.ClientId client)
    {
        if (_participants.Remove(client, out var entry))
        {
            if (entry.VehicleId.IsValid())
            {
                re.EntitySystem.Delete(entry.VehicleId);
            }

            Console.WriteLine(
                $"[Racing] Client {client} disconnected - {_participants.Count} participants remaining"
            );

            if (_participants.Count > 0)
                BroadcastPositions();
        }
    }

    public double GetRemainingRuntime()
    {
        return timeRaceMaxLength - (TimingHelper.GetTimeSeconds() - _raceStartTime);
    }

    public int GetNumberOfPlayers()
    {
        return _participants.Count;
    }

    public void OnCheckpointPassed(re.ClientId clientId, int checkpointIndex)
    {
        if (_track == null)
        {
            return;
        }

        _participants.TryGetValue(clientId, out var entry);
        if (entry != null)
        {
            entry.CurrentCheckpoint += 1;
            entry.LastCheckpointPassedTime = TimingHelper.GetTimeSeconds();
            Console.WriteLine($"[Racing] Client {clientId} passed checkpoint {checkpointIndex}");

            if (entry.CurrentCheckpoint >= _track.checkpoints.Length * _track.totalLaps)
            {
                entry.FinishedRace = true;

                if (_raceEndTime == 0.0)
                {
                    _raceEndTime = entry.LastCheckpointPassedTime + timeToFinishRace;
                    Console.WriteLine(
                        $"[Racing] Client {clientId} won. Ending match in {timeToFinishRace} seconds"
                    );
                }
            }

            BroadcastPositions();
        }
    }

    private void BroadcastPositions()
    {
        var sortedParticipants = _participants
            .OrderByDescending(kvp => kvp.Value.CurrentCheckpoint)
            .ThenBy(kvp => kvp.Value.LastCheckpointPassedTime)
            .ToList();

        var result = new List<types.racing.LeaderboardEntry>();
        foreach (var entry in sortedParticipants)
        {
            result.Add(
                new types.racing.LeaderboardEntry(
                    entry.Value.Name,
                    entry.Value.FinishedRace
                        ? entry.Value.LastCheckpointPassedTime - _raceActiveStartTime
                        : null
                )
            );
        }

        double? remainingRaceSeconds =
            _raceEndTime > 0.0 ? (_raceEndTime - TimingHelper.GetTimeSeconds()) : null;
        bool isRaceFinished = _state == RaceState.Ending;

        int position = 1;
        foreach (var entry in sortedParticipants)
        {
            re.Events.EmitNetTargeted(
                types.racing.MessageNames.RacePositionUpdate,
                entry.Key,
                new types.racing.RacePositionPayload(
                    position++,
                    sortedParticipants.Count,
                    remainingRaceSeconds
                )
            );

            bool isRaceFinishedForPlayer = entry.Value.FinishedRace || isRaceFinished;
            if (isRaceFinishedForPlayer)
            {
                re.Events.EmitNetTargeted(
                    types.racing.MessageNames.LeaderboardUpdate,
                    entry.Key,
                    new types.racing.RaceLeaderboardUpdatePayload(
                        result.ToArray(),
                        remainingRaceSeconds,
                        isRaceFinishedForPlayer
                    )
                );
            }
        }
    }

    public void OnRequestVehicle(re.ClientId clientId)
    {
        if (!_participants.TryGetValue(clientId, out var entry))
            return;

        var randomAppearance = VehicleManager.GetRandomAppearanceForVehicle(_randomVehicle);
        Console.WriteLine($"[Racing] Seletect appearance {randomAppearance} for client {clientId}");

        re.EntitySystem.Create(
            _randomVehicle,
            re.EntityType.Vehicle,
            clientId,
            entry.StartGridPosition.position,
            entry.StartGridPosition.rotation.ToQuaternion(),
            entity =>
            {
                if (entity == null)
                {
                    Console.WriteLine($"[Racing] Failed to spawn vehicle for client {clientId}");
                    re.GameClientRegistry.DisconnectClient(
                        clientId,
                        "Failed to spawn race vehicle"
                    );
                    return;
                }

                Console.WriteLine($"[Racing] Vehicle spawned for client {clientId}: {entity}");
                entry.VehicleId = entity;

                re.Events.EmitNetTargeted(
                    types.racing.MessageNames.RaceVehicleReady,
                    clientId,
                    new types.racing.RaceVehicleReadyPayload(entity)
                );
            },
            randomAppearance
        );
    }

    public void OnConfirmMounting(re.ClientId clientId)
    {
        _participants.TryGetValue(clientId, out var entry);
        if (entry != null)
        {
            entry.ReadyForRace = true;
            Console.WriteLine($"[Racing] Client {clientId} ready for race");
        }
    }
}
