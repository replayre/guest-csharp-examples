namespace GameModes.Client.Modes
{
    public class Racing : IGameMode
    {
        private const float defaultDetectionRadius = 11f;

        public enum RaceState
        {
            Uninitialized,
            Starting,
            WaitingForVehicle,
            Active,
            Ending,
        }

        private RaceState _state;
        private types.racing.Track? _currentRaceTrack = null;
        private List<types.racing.LeaderboardEntry> _leaderboard = new();
        private readonly List<(ent.Entity, game.NewMappinID)> _checkpointObjects = [];
        private double _lastHudUpdateTime = 0.0;
        private double _raceEndTime = 0.0;
        private double _lapStartTime = 0.0;
        private double _bestLapTime = 0.0;
        private int _nextCheckpointIndex = 0;
        private int _localCheckpointsPassed = 0;
        private int _currentLap = 1;
        private int _currentParticipants = 1;
        private int _currentPosition = 1;
        private game.NewMappinID? _gpsTargetMappin = null;

        public void OnActivate()
        {
            Console.WriteLine("[Racing] Mode activated - waiting for grid assignment");

            SessionManager.Revive();
            VehicleHelper.ForcePlayerExitVehicle();
        }

        public async void OnDeactivate()
        {
            var instance = ScriptGameInstance.Get();
            var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
            var player = playerSys?.GetLocalPlayerControlledGameObject();
            if (player == null)
            {
                return;
            }

            CleanupRaceCheckpoints();
            NativeRaceHudManager.EndRace();

            VehicleHelper.ForcePlayerExitVehicle();

            await Future.Yield();

            StatusEffectHelper.RemoveStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoDriving"),
                0
            );
            StatusEffectHelper.RemoveStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoCombat"),
                0
            );
            StatusEffectHelper.RemoveStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.VehicleBlockExit"),
                0
            );

            re.Ui.SendMessage(
                new UIMessage<types.racing.RaceHudPayload>(
                    types.racing.MessageNames.RaceHudUpdate,
                    new types.racing.RaceHudPayload(
                        false,
                        false,
                        false,
                        0,
                        0,
                        0.0,
                        0.0,
                        "",
                        null,
                        []
                    )
                )
            );
        }

        public void OnTick()
        {
            if (_currentRaceTrack == null)
            {
                return;
            }

            var instance = ScriptGameInstance.Get();
            var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;
            var mappinSys = ScriptGameInstance.GetMappinSystem(instance)!;

            var player = playerSys.GetLocalPlayerControlledGameObject();
            if (player == null)
            {
                return;
            }

            var playerPos = player.GetWorldPosition().ToVector3();

            var timeNow = TimingHelper.GetTimeSeconds();

            if (_state == RaceState.Starting)
            {
                var gridSlot = _currentRaceTrack.gridPositions[_currentPosition];
                if ((playerPos - gridSlot.position).LengthSquared() < 4.0f)
                {
                    Console.WriteLine($"[Racing] Teleport to starting position completed");

                    re.Events.EmitNet(types.racing.MessageNames.RequestVehicle);
                    _state = RaceState.WaitingForVehicle;
                }
            }
            else if (_state == RaceState.Active)
            {
                var totalCheckpoints = _currentRaceTrack.checkpoints.Length;
                var cp = _currentRaceTrack.checkpoints[_nextCheckpointIndex];

                var radius = cp.detectionRadius ?? defaultDetectionRadius;
                var distance = (playerPos - cp.position).LengthSquared();
                if (distance < radius * radius)
                {
                    Console.WriteLine(
                        $"[Racing] Passed Checkpoint {_nextCheckpointIndex + 1}/{totalCheckpoints} in lap {_currentLap}/{_currentRaceTrack.totalLaps}"
                    );

                    game.Object.PlaySoundEvent(player, new CName("time_dilation_focused_exit"));

                    re.Events.EmitNet(
                        types.racing.MessageNames.RaceCheckpointPassed,
                        new types.racing.RaceCheckpointPassedPayload(_nextCheckpointIndex)
                    );

                    _nextCheckpointIndex = (_nextCheckpointIndex + 1) % totalCheckpoints;
                    _localCheckpointsPassed++;

                    if (_gpsTargetMappin != null)
                    {
                        RegisterTrackingPointMarker(_nextCheckpointIndex);

                        mappinSys.SetMappinPosition(
                            _gpsTargetMappin,
                            _currentRaceTrack.checkpoints[_nextCheckpointIndex].position.ToVector4()
                        );
                    }

                    NativeRaceHudManager.OnCheckpointPassed(_localCheckpointsPassed);

                    if (
                        _localCheckpointsPassed % totalCheckpoints == 0
                        && _currentRaceTrack.totalLaps > 1
                    )
                    {
                        var lapTime = timeNow - _lapStartTime;
                        if (_bestLapTime == 0.0 || lapTime < _bestLapTime)
                        {
                            _bestLapTime = lapTime;
                        }

                        _currentLap++;
                        _lapStartTime = timeNow;
                    }

                    _lastHudUpdateTime = 0.0;
                }

                NativeRaceHudManager.Tick(_currentPosition, _currentParticipants);
            }
            else if (_state == RaceState.Ending)
            {
                StatusEffectHelper.ApplyStatusEffect(
                    player.Downgrade(),
                    new TDBID("GameplayRestriction.NoDriving"),
                    0
                );

                NativeRaceHudManager.EndRace();
            }

            if ((timeNow - _lastHudUpdateTime) > 0.1)
            {
                SendHudUpdate();
                _lastHudUpdateTime = timeNow;
            }
        }

        public async void OnRaceSetup(types.racing.Track track, int slotIndex, int totalPlayers)
        {
            Console.WriteLine($"[Racing] OnRaceSetup {track.name} {slotIndex}");

            _currentPosition = slotIndex;
            _currentParticipants = totalPlayers;
            _currentRaceTrack = track;
            SetupRaceCheckpoints();

            var gridSlot = _currentRaceTrack.gridPositions[slotIndex];
            EntityHelper.Teleport(gridSlot.position.ToVector4(), gridSlot.rotation.ToQuaternion());

            await Future.Yield();

            _state = RaceState.Starting;
        }

        public async void OnVehicleReady(re.EntityId entityId)
        {
            Console.WriteLine($"[Racing] OnVehicleReady");

            var instance = ScriptGameInstance.Get();
            var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
            var player = playerSys?.GetLocalPlayerControlledGameObject();
            if (player == null)
            {
                return;
            }

            var entity = re.EntitySystem.GetEntityIdFromNetId(entityId);
            if (entity == null)
            {
                Console.WriteLine(
                    $"[Racing] Failed to get entity for vehicle with netId {entityId}"
                );
                return;
            }

            VehicleHelper.EnterVehicle(entity);

            await Future.Yield();

            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoDriving"),
                0
            );
            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoCombat"),
                0
            );
            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.VehicleBlockExit"),
                0
            );
            Console.WriteLine($"[Racing] Seated player in vehicle");

            re.Events.EmitNet(types.racing.MessageNames.ConfirmMounting);
            _state = RaceState.Active;
        }

        public void OnCountdown()
        {
            Console.WriteLine($"[Racing] OnCountdown");

            _lapStartTime = TimingHelper.GetTimeSeconds() + 5.0;

            NativeRaceHudManager.PreRaceSetup(
                _currentRaceTrack!.checkpoints.Length * _currentRaceTrack.totalLaps,
                _currentParticipants,
                _currentPosition
            );

            ScriptGameInstance
                .GetUISystem(ScriptGameInstance.Get())
                ?.QueueEvent(
                    new ForwardVehicleRaceUIEvent { mode = vehicle.RaceUI.CountdownStart }
                );
        }

        public void OnRaceStart()
        {
            Console.WriteLine($"[Racing] OnRaceStart");

            var instance = ScriptGameInstance.Get();
            var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
            var player = playerSys?.GetLocalPlayerControlledGameObject();
            if (player == null)
            {
                return;
            }

            StatusEffectHelper.RemoveStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoDriving"),
                0
            );

            NativeRaceHudManager.StartRace();
        }

        public void OnPositionUpdate(types.racing.RacePositionPayload payload)
        {
            Console.WriteLine(
                $"[Racing] OnPositionUpdate {payload.Position}/{payload.TotalPlayers}"
            );

            if (payload.RaceRemainingSeconds != null)
            {
                _raceEndTime = (double)(
                    TimingHelper.GetTimeSeconds() + payload.RaceRemainingSeconds
                );
            }

            _currentPosition = payload.Position;
            _currentParticipants = payload.TotalPlayers;
            _lastHudUpdateTime = 0.0;
        }

        public void OnLeaderboardUpdate(types.racing.RaceLeaderboardUpdatePayload payload)
        {
            Console.WriteLine(
                $"[Racing] OnLeaderboardUpdate {payload.RaceFinished} {payload.RaceRemainingSeconds}"
            );

            if (payload.RaceRemainingSeconds != null)
            {
                _raceEndTime = (double)(
                    TimingHelper.GetTimeSeconds() + payload.RaceRemainingSeconds
                );
            }

            _leaderboard = payload.Entries.ToList();

            if (payload.RaceFinished && _state == RaceState.Active)
            {
                _state = RaceState.Ending;
            }

            _lastHudUpdateTime = 0.0;
        }

        private void SendHudUpdate()
        {
            if (_currentRaceTrack == null)
            {
                return;
            }

            var timeNow = TimingHelper.GetTimeSeconds();
            var timerSeconds = _lapStartTime > 0.0 ? timeNow - _lapStartTime : 0.0;

            re.Ui.SendMessage(
                new UIMessage<types.racing.RaceHudPayload>(
                    types.racing.MessageNames.RaceHudUpdate,
                    new types.racing.RaceHudPayload(
                        IsActive: _state >= RaceState.Active,
                        IsDone: _state == RaceState.Ending,
                        IsSprint: _currentRaceTrack.totalLaps == 1,
                        CurrentLap: _currentLap,
                        TotalLaps: _currentRaceTrack.totalLaps,
                        LapTimeSeconds: timerSeconds,
                        BestLapTimeSeconds: _bestLapTime,
                        TrackName: _currentRaceTrack.name,
                        EndTime: _raceEndTime > 0 ? Math.Max(_raceEndTime - timeNow, 0) : null,
                        Leaderboard: _leaderboard.ToArray()
                    )
                )
            );
        }

        private void SetupRaceCheckpoints()
        {
            var instance = ScriptGameInstance.Get();
            var mappinSys = ScriptGameInstance.GetMappinSystem(instance)!;

            if (_currentRaceTrack == null)
            {
                return;
            }

            CleanupRaceCheckpoints();
            for (var i = 0; i < _currentRaceTrack.checkpoints.Length; i++)
            {
                var checkpoint = _currentRaceTrack.checkpoints[i];
                var entityName = GetCheckpointEntityName(checkpoint.type);
                if (entityName == null)
                {
                    Console.WriteLine($"[Racing] Unknown checkpoint type: {checkpoint.type}");
                    continue;
                }

                re.EntitySystem.Create(
                    entityName,
                    checkpoint.position,
                    checkpoint.rotation.ToQuaternion(),
                    entity =>
                    {
                        if (entity == null)
                        {
                            Console.WriteLine($"[Racing] Failed to create entity for {entityName}");
                            return;
                        }

                        var data = new game.map.pins.MappinData
                        {
                            mappinType = new TDBID("Mappins.StaticPointOfInterestMappinDefinition"),
                            variant = game.data.MappinVariant.Zzz18_RacingVariant,
                            active = true,
                            visibleThroughWalls = true,
                        };

                        var mappinID = mappinSys.RegisterMappin(
                            data,
                            checkpoint.position.ToVector4()
                        );

                        _checkpointObjects.Add((entity, mappinID));
                    },
                    false
                );
            }

            RegisterTrackingPointMarker(0);
        }

        private void CleanupRaceCheckpoints()
        {
            var instance = ScriptGameInstance.Get();
            var mappinSys = ScriptGameInstance.GetMappinSystem(instance)!;

            _checkpointObjects.ForEach(entry =>
            {
                entry.Item1?.Dispose();
                mappinSys.UnregisterMappin(entry.Item2);
            });
            _checkpointObjects.Clear();
        }

        private void RegisterTrackingPointMarker(int checkpointIndex)
        {
            var instance = ScriptGameInstance.Get();
            var mappinSys = ScriptGameInstance.GetMappinSystem(instance)!;

            if (_gpsTargetMappin != null)
            {
                var pin = mappinSys.GetMappin(_gpsTargetMappin);
                if (pin != null)
                {
                    return;
                }
            }

            if (checkpointIndex > _currentRaceTrack?.checkpoints.Length)
            {
                return;
            }

            var data = new game.map.pins.MappinData
            {
                mappinType = new TDBID("Mappins.CustomPositionMappinDefinition"),
                variant = game.data.MappinVariant.CustomPositionVariant,
                active = true,
                visibleThroughWalls = true,
            };

            _gpsTargetMappin = mappinSys.RegisterMappin(
                data,
                _currentRaceTrack!.checkpoints[checkpointIndex].position.ToVector4()
            );
        }

        private static string? GetCheckpointEntityName(types.racing.CheckpointType type) =>
            type switch
            {
                types.racing.CheckpointType.StartLine =>
                    @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint.ent",
                types.racing.CheckpointType.Standard =>
                    @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint.ent",
                types.racing.CheckpointType.RightTurn =>
                    @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint_right.ent",
                types.racing.CheckpointType.LeftTurn =>
                    @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint_left.ent",
                types.racing.CheckpointType.FinishLine =>
                    @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint.ent",
                _ => null,
            };
    }
}
