namespace types.lobby;

[Serde.GenerateSerde]
public partial record HudPayload(
    bool IsClientInLobbyMode,
    bool IsGameModeActive,
    bool IsGameModeJoinable,
    string CurrentGameMode,
    int CurrentGameModeRemainingDuration,
    int CurrentGameModeNumberOfPlayers
);

public static class MessageNames
{
    public const string HudUpdate = "lobby:hudUpdate";
}
