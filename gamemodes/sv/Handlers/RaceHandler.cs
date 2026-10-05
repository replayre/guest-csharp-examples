namespace GameModes.Server;

public static class RaceHandler
{
    public static void Register()
    {
        re.Events.OnNet(
            types.racing.MessageNames.RequestVehicle,
            (clientId) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnRequestVehicle(clientId);
                }
            }
        );

        re.Events.OnNet(
            types.racing.MessageNames.ConfirmMounting,
            (clientId) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnConfirmMounting(clientId);
                }
            }
        );

        re.Events.OnNet<types.racing.RaceCheckpointPassedPayload>(
            types.racing.MessageNames.RaceCheckpointPassed,
            (clientId, payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnCheckpointPassed(clientId, payload.CheckpointIndex);
                }
            }
        );
    }
}
