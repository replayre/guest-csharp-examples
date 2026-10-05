namespace types.racing;

[Serde.GenerateSerde]
[Serde.SerdeTypeOptions(MemberFormat = Serde.MemberFormat.PascalCase)]
public enum CheckpointType
{
    StartLine,
    Standard,
    LeftTurn,
    RightTurn,
    FinishLine,
}

[Msgpack.Gen, Serde.GenerateSerde]
public partial record Checkpoint(
    Vector3 position,
    EulerAngles rotation,
    CheckpointType type = CheckpointType.Standard,
    float? detectionRadius = null
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record GridPosition(Vector3 position, EulerAngles rotation);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record Track(
    string id,
    string name,
    int totalLaps,
    Checkpoint[] checkpoints,
    GridPosition[] gridPositions
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record LeaderboardEntry(string Name, double? Score);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record RaceHudPayload(
    bool IsActive,
    bool IsDone,
    bool IsSprint,
    int CurrentLap,
    int TotalLaps,
    double LapTimeSeconds,
    double BestLapTimeSeconds,
    string TrackName,
    double? EndTime,
    LeaderboardEntry[] Leaderboard
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record RaceSetupPayload(Track Track, int SlotIndex, int TotalPlayers);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record RacePositionPayload(
    int Position,
    int TotalPlayers,
    double? RaceRemainingSeconds
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record RaceCheckpointPassedPayload(int CheckpointIndex);

[Msgpack.Gen]
public partial record RaceVehicleReadyPayload(re.EntityId EntityId);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record RaceLeaderboardUpdatePayload(
    LeaderboardEntry[] Entries,
    double? RaceRemainingSeconds,
    bool RaceFinished
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record RaceTrackRequestPayload();

[Msgpack.Gen, Serde.GenerateSerde]
public partial record RaceTracksResponsePayload(Track[] Tracks);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record SaveGridPositionsPayload(
    string TrackId,
    GridPosition[] Positions
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record SaveTrackPayload(Track Track);

public static class MessageNames
{
    public const string RaceSetup = "race:setup";
    public const string RequestVehicle = "race:requestVehicle";
    public const string ConfirmMounting = "race:confirmMounting";
    public const string RaceHudUpdate = "race:hudUpdate";
    public const string RaceCheckpointPassed = "race:checkpointPassed";
    public const string RaceCountdown = "race:countdown";
    public const string RaceStart = "race:start";
    public const string RaceVehicleReady = "race:vehicleReady";
    public const string RacePositionUpdate = "race:positionUpdate";
    public const string LeaderboardUpdate = "race:leaderboardUpdate";
    public const string RaceTracksRequest = "race:tracksRequest";
    public const string RaceTracksResponse = "race:tracksResponse";
    public const string SaveGridPositions = "race:saveGridPositions";
    public const string SaveTrack = "race:saveTrack";
}
