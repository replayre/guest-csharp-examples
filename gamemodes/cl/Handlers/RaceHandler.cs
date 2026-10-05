namespace GameModes.Client;

public static class RaceHandler
{
    public static void Register()
    {
        re.Events.OnNet<types.racing.RaceSetupPayload>(
            types.racing.MessageNames.RaceSetup,
            (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnRaceSetup(payload.Track, payload.SlotIndex, payload.TotalPlayers);
                }
            }
        );

        re.Events.OnNet<types.racing.RaceVehicleReadyPayload>(
            types.racing.MessageNames.RaceVehicleReady,
            (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnVehicleReady(payload.EntityId);
                }
            }
        );

        re.Events.OnNet(
            types.racing.MessageNames.RaceCountdown,
            () =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnCountdown();
                }
            }
        );

        re.Events.OnNet(
            types.racing.MessageNames.RaceStart,
            () =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnRaceStart();
                }
            }
        );

        re.Events.OnNet<types.racing.RacePositionPayload>(
            types.racing.MessageNames.RacePositionUpdate,
            (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnPositionUpdate(payload);
                }
            }
        );

        re.Events.OnNet<types.racing.RaceLeaderboardUpdatePayload>(
            types.racing.MessageNames.LeaderboardUpdate,
            (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Racing racing)
                {
                    racing.OnLeaderboardUpdate(payload);
                }
            }
        );
    }
}
