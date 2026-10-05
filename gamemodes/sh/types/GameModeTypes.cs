namespace types.gamemodes;

[Msgpack.Gen, Serde.GenerateSerde]
public partial record GameModeOption(
    string Id,
    string Name,
    bool Hidden,
    string? Description = null,
    string? Icon = null
);

[Msgpack.Gen]
public partial record ModeActivated(string id);

[Msgpack.Gen]
public partial record ActiveModeUpdate(
    bool IsGameModeActive,
    bool IsGameModeJoinable,
    string CurrentGameMode,
    int CurrentGameModeRemainingDuration,
    int CurrentGameModeNumberOfPlayers
);

[Msgpack.Gen]
public partial record DebugModeUpdate(bool Enabled);

public static class MessageNames
{
    public const string ClientReady = "gamemode:clientReady";
    public const string ModeActivated = "gamemode:modeActivated";
    public const string ActiveModeUpdate = "gamemode:activeModeUpdate";
    public const string DebugModeUpdate = "gamemode:debugModeUpdate";
    public const string JoinDelayed = "gamemode:joinDelayed";
}
