namespace GameModes.Server;

public static class DeathMatchHandler
{
    public static void Register()
    {
        re.Events.OnNet(
            types.dm.MessageNames.ReadyForMatch,
            (clientId) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnClientReadyForMatch(clientId);
                }
            }
        );

        re.Events.OnNet(
            types.dm.MessageNames.RespawnCompleted,
            (clientId) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnClientRespawned(clientId);
                }
            }
        );

        re.Events.OnNet<types.dm.PlayerKilledPayload>(
            types.dm.MessageNames.PlayerKilled,
            (clientId, payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnPlayerKilled(clientId, payload);
                }
            }
        );

        re.Events.OnNet(
            types.dm.MessageNames.PlayerDeath,
            (clientId) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnClientDeath(clientId);
                }
            }
        );
    }
}
