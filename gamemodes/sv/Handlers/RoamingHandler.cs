namespace GameModes.Server;

public static class RoamingHandler
{
    public static void Register()
    {
        re.Events.OnNet<types.roaming.UpdateCombatStatePayload>(
            types.roaming.MessageNames.UpdateCombatState,
            async (clientId, payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnUpdateCombatState(clientId, payload);
                }
            }
        );

        re.Events.OnNet(
            types.roaming.MessageNames.PlayerRequestVehicle,
            async (clientId) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnPlayerRequestVehicle(clientId);
                }
            }
        );
    }
}
