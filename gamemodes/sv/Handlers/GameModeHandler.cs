namespace GameModes.Server;

public static class GameModeHandler
{
    public static void Register()
    {
        re.Events.OnNet(types.gamemodes.MessageNames.ClientReady, GameModeManager.OnClientReady);

        re.Events.OnNet(
            types.gamemodes.MessageNames.JoinDelayed,
            GameModeManager.OnClientRequestDelayedJoin
        );

        re.Events.On<re.ClientObserverEntityChanged>(
            (payload) =>
                GameModeManager.OnObserverEntityChanged(
                    payload.id,
                    payload.previous,
                    payload.current
                )
        );
    }
}
