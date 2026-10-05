namespace types.dm;

[Msgpack.Gen, Serde.GenerateSerde]
public partial record LeaderboardEntry(string Name, int? Score);

[Serde.GenerateSerde]
public partial record HudPayload(
    int MatchState,
    string MatchTypeName,
    int CountdownRemainingSeconds,
    int MatchRemainingSeconds,
    LeaderboardEntry[] leaderboard,
    bool OutsideArena,
    int OutsideArenaRemainingTime
);

[Msgpack.Gen]
public partial record ArenaBorderViolation(double violationTimer);

[Msgpack.Gen]
public partial record MatchBeginPayload(double countdownSeconds, double matchRemainingSeconds);

[Msgpack.Gen]
public partial record MatchSyncPayload(
    double matchRemainingSeconds,
    LeaderboardEntry[] LeaderboardEntries
);

[Msgpack.Gen]
public partial record PlayerKilledPayload(re.EntityId killer);

public static class MessageNames
{
    public const string MatchPrepare = "dm:matchPrepare";
    public const string MatchBegin = "dm:matchBegin";
    public const string ReadyForMatch = "dm:readyForMatch";
    public const string MatchSync = "dm:matchSync";
    public const string MatchEnding = "dm:matchEnding";
    public const string RespawnPlayer = "dm:respawnPlayer";
    public const string RespawnCompleted = "dm:respawnCompleted";
    public const string KillConfirmed = "dm:killConfirmed";
    public const string ArenaBorderViolation = "dm:arenaBorderViolation";
    public const string ForceKill = "dm:forceKill";
    public const string PlayerKilled = "dm:playerKilled";
    public const string PlayerDeath = "dm:playerDeath";
    public const string HudUpdate = "dm:hudUpdate";
}
