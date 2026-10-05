namespace GameModes.Client;

public static class DeathMatchHandler
{
    public static void Register()
    {
        re.Events.OnNet<types.arena.SpawnPoint>(
            types.dm.MessageNames.MatchPrepare,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnMatchPrepare(payload);
                }
            }
        );

        re.Events.OnNet<types.dm.MatchBeginPayload>(
            types.dm.MessageNames.MatchBegin,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnMatchBegin(payload);
                }
            }
        );

        re.Events.OnNet<types.dm.MatchSyncPayload>(
            types.dm.MessageNames.MatchSync,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnMatchSync(payload);
                }
            }
        );

        re.Events.OnNet(
            types.dm.MessageNames.MatchEnding,
            async () =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnMatchEnding();
                }
            }
        );

        re.Events.OnNet<types.arena.SpawnPoint>(
            types.dm.MessageNames.RespawnPlayer,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnRespawnRequested(payload);
                }
            }
        );

        re.Events.OnNet(
            types.dm.MessageNames.KillConfirmed,
            async () =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnKillConfirmed();
                }
            }
        );

        re.Events.OnNet<types.dm.ArenaBorderViolation>(
            types.dm.MessageNames.ArenaBorderViolation,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnClientArenaViolation(payload);
                }
            }
        );

        re.Events.OnNet(
            types.dm.MessageNames.ForceKill,
            async () =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.DeathMatch dm)
                {
                    dm.OnClientForceKilled();
                }
            }
        );
    }
}
