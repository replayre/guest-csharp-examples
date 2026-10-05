namespace GameModes.Client;

public static class GameModeHandler
{
    public static void Register()
    {
        re.Events.OnNet<types.gamemodes.ModeActivated>(
            types.gamemodes.MessageNames.ModeActivated,
            (payload) =>
            {
                GameModeManager.ActivateGameMode(payload.id);
            }
        );

        re.Events.OnNet<types.gamemodes.ActiveModeUpdate>(
            types.gamemodes.MessageNames.ActiveModeUpdate,
            (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Lobby lobby)
                {
                    lobby.OnActiveModeUpdate(payload);
                }
            }
        );

        re.Con.RegisterCommandWithKeyBind(
            "join-existing-mode",
            (string[] args) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Lobby lobby)
                {
                    if (lobby._modeInfo.IsGameModeJoinable)
                    {
                        re.Events.EmitNet(types.gamemodes.MessageNames.JoinDelayed);
                    }
                }
            },
            EInputKey.IK_F,
            "Join existing Mode"
        );
    }
}
