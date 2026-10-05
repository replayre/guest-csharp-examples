using types.racing;

namespace cl.DebugTools;

public static class RacingTool
{
    private static string? _gridTrackId = null;
    private static readonly List<GridPosition> Positions = [];
    private static readonly List<ent.Entity> ReferenceEntities = [];
    private static readonly Dictionary<string, Track> _knownTracks = [];
    private static Action? _onTracksLoaded = null;
    private static ent.Entity? _gridStartEntity = null;

    private static string? _trackId = null;
    private static string? _trackName = null;
    private static int _trackLaps = 1;
    private static readonly List<Checkpoint> _trackCheckpoints = [];
    private static readonly List<ent.Entity> _trackCheckpointEntities = [];

    private static Track? _stepTrack = null;
    private static int _stepIndex = -1;
    private static ent.Entity? _stepEntity = null;

    public static void Register()
    {
        re.Events.OnNet<RaceTracksResponsePayload>(
            MessageNames.RaceTracksResponse,
            (payload) =>
            {
                _knownTracks.Clear();
                foreach (var t in payload.Tracks)
                    _knownTracks[t.id] = t;

                Console.WriteLine($"[GridRecorder] Loaded {payload.Tracks.Length} track(s)");
                _onTracksLoaded?.Invoke();
                _onTracksLoaded = null;
            }
        );

        re.Con.RegisterCommand(
            "grid::start",
            args =>
            {
                var trackId = args.FirstOrDefault();
                if (string.IsNullOrEmpty(trackId))
                {
                    Console.WriteLine("Usage: grid::start <trackId>");
                    return;
                }

                if (_gridTrackId != null)
                {
                    Console.WriteLine("[GridRecorder] Already recording — use grid::clear first");
                    return;
                }

                StartGrid(trackId);
            }
        );

        re.Con.RegisterCommand(
            "grid::record",
            args =>
            {
                if (_gridTrackId == null)
                {
                    Console.WriteLine(
                        "[GridRecorder] Not recording — use grid::start <trackId> first"
                    );
                    return;
                }
                Record();
            }
        );

        re.Con.RegisterCommand(
            "grid::save",
            args =>
            {
                if (_gridTrackId == null)
                {
                    Console.WriteLine(
                        "[GridRecorder] Not recording — use grid::start <trackId> first"
                    );
                    return;
                }
                Send();
            }
        );

        re.Con.RegisterCommand(
            "grid::clear",
            args =>
            {
                _gridTrackId = null;
                Positions.Clear();
                ClearGridEntities();
                Console.WriteLine("[GridRecorder] Stopped and cleared");
            }
        );

        re.Con.RegisterCommand(
            "track::begin",
            args =>
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: track::begin <id> <name> [laps]");
                    return;
                }

                if (_trackId != null)
                {
                    Console.WriteLine(
                        "[TrackRecorder] Already recording — use track::end or track::clear first"
                    );
                    return;
                }

                _trackId = args[0];
                _trackName = args[1];
                _trackLaps = args.Length >= 3 && int.TryParse(args[2], out var laps) ? laps : 1;
                _trackCheckpoints.Clear();
                ClearTrackEntities();

                Console.WriteLine(
                    $"[TrackRecorder] Started recording track '{_trackName}' ({_trackId}), laps={_trackLaps}"
                );
            }
        );

        re.Con.RegisterCommand(
            "track::checkpoint",
            args =>
            {
                if (_trackId == null)
                {
                    Console.WriteLine("[TrackRecorder] Not recording — use track::begin first");
                    return;
                }

                var typeStr = args.FirstOrDefault()?.ToLowerInvariant() ?? "standard";
                var cpType = typeStr switch
                {
                    "left" => CheckpointType.LeftTurn,
                    "right" => CheckpointType.RightTurn,
                    "start" => CheckpointType.StartLine,
                    "finish" => CheckpointType.FinishLine,
                    _ => CheckpointType.Standard,
                };

                var instance = ScriptGameInstance.Get();
                var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
                var player = playerSys?.GetLocalPlayerControlledGameObject();
                if (player == null)
                {
                    Console.WriteLine("[TrackRecorder] No local player found");
                    return;
                }

                var pos = player.GetWorldPosition();
                var ori = player.GetWorldOrientation();

                var checkpoint = new Checkpoint(pos.ToVector3(), ori.ToEuler(), cpType);
                _trackCheckpoints.Add(checkpoint);

                var entityPath = GetCheckpointEntityPath(cpType);
                re.EntitySystem.Create(
                    entityPath,
                    checkpoint.position,
                    checkpoint.rotation.ToQuaternion(),
                    entity =>
                    {
                        if (entity != null)
                            _trackCheckpointEntities.Add(entity);
                        else
                            Console.WriteLine($"[TrackRecorder] Failed to spawn checkpoint entity");
                    },
                    false
                );

                Console.WriteLine(
                    $"[TrackRecorder] Checkpoint #{_trackCheckpoints.Count} ({cpType}): pos=({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})"
                );
            }
        );

        re.Con.RegisterCommand(
            "track::undo",
            args =>
            {
                if (_trackId == null)
                {
                    Console.WriteLine("[TrackRecorder] Not recording");
                    return;
                }

                if (_trackCheckpoints.Count == 0)
                {
                    Console.WriteLine("[TrackRecorder] No checkpoints to undo");
                    return;
                }

                _trackCheckpoints.RemoveAt(_trackCheckpoints.Count - 1);

                if (_trackCheckpointEntities.Count > 0)
                {
                    var last = _trackCheckpointEntities[^1];
                    last?.Dispose();
                    _trackCheckpointEntities.RemoveAt(_trackCheckpointEntities.Count - 1);
                }

                Console.WriteLine(
                    $"[TrackRecorder] Undone — {_trackCheckpoints.Count} checkpoints remaining"
                );
            }
        );

        re.Con.RegisterCommand(
            "track::end",
            args =>
            {
                if (_trackId == null)
                {
                    Console.WriteLine("[TrackRecorder] Not recording");
                    return;
                }

                if (_trackCheckpoints.Count == 0)
                {
                    Console.WriteLine("[TrackRecorder] No checkpoints recorded — nothing to save");
                    return;
                }

                var track = new Track(
                    _trackId,
                    _trackName!,
                    _trackLaps,
                    [.. _trackCheckpoints],
                    [.. Positions]
                );

                re.Events.EmitNet(MessageNames.SaveTrack, new SaveTrackPayload(track));
                Console.WriteLine(
                    $"[TrackRecorder] Sent track '{_trackName}' with {_trackCheckpoints.Count} checkpoints to server"
                );

                _trackId = null;
                _trackName = null;
                _trackLaps = 1;
                _trackCheckpoints.Clear();
                ClearTrackEntities();
            }
        );

        re.Con.RegisterCommand(
            "track::clear",
            args =>
            {
                _trackId = null;
                _trackName = null;
                _trackLaps = 1;
                _trackCheckpoints.Clear();
                ClearTrackEntities();
                Console.WriteLine("[TrackRecorder] Cleared track recording");
            }
        );

        re.Con.RegisterCommand(
            "track::walk",
            args =>
            {
                var trackId = args.FirstOrDefault();
                if (string.IsNullOrEmpty(trackId))
                {
                    Console.WriteLine("Usage: track::walk <trackId>");
                    return;
                }

                if (_knownTracks.Count == 0)
                {
                    Console.WriteLine("[TrackWalk] No tracks loaded — requesting from server...");
                    _onTracksLoaded = () =>
                    {
                        // re-invoke after load
                        StartWalk(trackId);
                    };
                    re.Events.EmitNet(
                        MessageNames.RaceTracksRequest,
                        new RaceTrackRequestPayload()
                    );
                    return;
                }

                StartWalk(trackId);
            }
        );

        re.Con.RegisterCommand(
            "track::next",
            _ =>
            {
                if (_stepTrack == null)
                {
                    Console.WriteLine("[TrackWalk] Not walking — use track::walk <trackId> first");
                    return;
                }

                _stepIndex++;
                if (_stepIndex >= _stepTrack.checkpoints.Length)
                {
                    Console.WriteLine(
                        $"[TrackWalk] Reached end of track '{_stepTrack.name}' ({_stepTrack.checkpoints.Length} checkpoints)"
                    );
                    ClearStepEntity();
                    _stepTrack = null;
                    _stepIndex = -1;
                    return;
                }

                StepToCheckpoint();
            }
        );

        re.Con.RegisterCommand(
            "track::prev",
            _ =>
            {
                if (_stepTrack == null)
                {
                    Console.WriteLine("[TrackWalk] Not walking — use track::walk <trackId> first");
                    return;
                }

                if (_stepIndex <= 0)
                {
                    Console.WriteLine("[TrackWalk] Already at first checkpoint");
                    return;
                }

                _stepIndex--;
                StepToCheckpoint();
            }
        );

        re.Con.RegisterCommand(
            "track::stopwalk",
            _ =>
            {
                ClearStepEntity();
                _stepTrack = null;
                _stepIndex = -1;
                Console.WriteLine("[TrackWalk] Stopped");
            }
        );

        re.Events.OnNet<types.gamemodes.DebugModeUpdate>(
            types.gamemodes.MessageNames.DebugModeUpdate,
            (payload) =>
            {
                GameModes.Client.GameModeManager.DebugMode = payload.Enabled;
                Console.WriteLine($"[Debug] Debug mode {(payload.Enabled ? "ON" : "OFF")}");
            }
        );
    }

    private static void StartGrid(string trackId)
    {
        if (_knownTracks.Count == 0)
        {
            Console.WriteLine("[GridRecorder] No tracks loaded — requesting from server...");
            _onTracksLoaded = () => StartGrid(trackId);
            re.Events.EmitNet(MessageNames.RaceTracksRequest, new RaceTrackRequestPayload());
            return;
        }

        if (!_knownTracks.TryGetValue(trackId, out var track))
        {
            Console.WriteLine($"[GridRecorder] Track '{trackId}' not found. Available tracks:");
            foreach (var (id, t) in _knownTracks)
                Console.WriteLine($"  - {id} ({t.name})");
            return;
        }

        _gridTrackId = trackId;
        Positions.Clear();
        ClearGridEntities();

        if (track.checkpoints.Length > 0)
        {
            var start = track.checkpoints[0];
            EntityHelper.Teleport(start.position.ToVector4(), start.rotation.ToQuaternion());

            re.EntitySystem.Create(
                GetCheckpointEntityPath(start.type),
                start.position,
                start.rotation.ToQuaternion(),
                entity =>
                {
                    _gridStartEntity = entity;
                },
                false
            );

            Console.WriteLine($"[GridRecorder] Teleported to first checkpoint of '{track.name}'");
        }

        Console.WriteLine(
            $"[GridRecorder] Started grid recording for track '{track.name}' ({trackId})"
        );
    }

    private static async void Record()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            Console.WriteLine("[GridRecorder] No local player found");
            return;
        }

        var pos = player.GetWorldPosition();
        var ori = player.GetWorldOrientation();
        var (roll, pitch, yaw) = ori.ToEuler();

        var gridPos = new GridPosition(pos.ToVector3(), ori.ToEuler());

        Positions.Add(gridPos);
        var slotIndex = Positions.Count - 1;
        var spawnPos = new Vector3(pos.X, pos.Y, pos.Z);

        await Future.Sleep(5000);

        re.EntitySystem.Create(
            new TDBID("Vehicle.v_sport1_rayfield_caliburn"),
            spawnPos,
            ori,
            entity =>
            {
                if (entity != null)
                    ReferenceEntities.Add(entity);
                else
                    Console.WriteLine(
                        $"[GridRecorder] Failed to spawn reference car at slot {slotIndex}"
                    );
            },
            false
        );

        Console.WriteLine(
            $"[GridRecorder] Recorded slot {Positions.Count - 1}: pos=({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}) rot=({roll:F1}, {pitch:F1}, {yaw:F1})"
        );
    }

    private static void ClearGridEntities()
    {
        foreach (var referenceEntity in ReferenceEntities)
            referenceEntity?.Dispose();
        ReferenceEntities.Clear();

        _gridStartEntity?.Dispose();
        _gridStartEntity = null;
    }

    private static void Send()
    {
        if (Positions.Count == 0)
        {
            Console.WriteLine("[GridRecorder] No positions recorded");
            return;
        }

        re.Events.EmitNet(
            MessageNames.SaveGridPositions,
            new SaveGridPositionsPayload(TrackId: _gridTrackId!, Positions: [.. Positions])
        );

        Console.WriteLine(
            $"[GridRecorder] Sent {Positions.Count} grid positions for track '{_gridTrackId}'"
        );

        _gridTrackId = null;
        Positions.Clear();
        ClearGridEntities();
    }

    private static void ClearTrackEntities()
    {
        foreach (var entity in _trackCheckpointEntities)
            entity?.Dispose();
        _trackCheckpointEntities.Clear();
    }

    private static void StartWalk(string trackId)
    {
        if (!_knownTracks.TryGetValue(trackId, out var track))
        {
            Console.WriteLine($"[TrackWalk] Track '{trackId}' not found. Available tracks:");
            foreach (var (id, t) in _knownTracks)
                Console.WriteLine($"  - {id} ({t.name})");
            return;
        }

        if (track.checkpoints.Length == 0)
        {
            Console.WriteLine($"[TrackWalk] Track '{track.name}' has no checkpoints");
            return;
        }

        ClearStepEntity();
        _stepTrack = track;
        _stepIndex = 0;
        Console.WriteLine(
            $"[TrackWalk] Walking track '{track.name}' — {track.checkpoints.Length} checkpoints. Use track::next / track::prev"
        );
        StepToCheckpoint();
    }

    private static void StepToCheckpoint()
    {
        var cp = _stepTrack!.checkpoints[_stepIndex];

        ClearStepEntity();
        EntityHelper.Teleport(cp.position.ToVector4(), cp.rotation.ToQuaternion());

        re.EntitySystem.Create(
            GetCheckpointEntityPath(cp.type),
            cp.position,
            cp.rotation.ToQuaternion(),
            entity =>
            {
                _stepEntity = entity;
            },
            false
        );

        var pos = cp.position;
        var rot = cp.rotation;
        Console.WriteLine(
            $"[TrackWalk] [{_stepIndex + 1}/{_stepTrack.checkpoints.Length}] {cp.type} — pos=({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}) rot=({rot.Roll:F1}, {rot.Pitch:F1}, {rot.Yaw:F1})"
        );
    }

    private static void ClearStepEntity()
    {
        _stepEntity?.Dispose();
        _stepEntity = null;
    }

    private static string GetCheckpointEntityPath(CheckpointType type) =>
        type switch
        {
            CheckpointType.RightTurn =>
                @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint_right.ent",
            CheckpointType.LeftTurn =>
                @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint_left.ent",
            _ => @"base\gameplay\devices\street_signs\race_checkpoint\race_checkpoint.ent",
        };
}
