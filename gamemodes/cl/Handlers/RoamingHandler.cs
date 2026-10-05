namespace GameModes.Client;

public static class RoamingHandler
{
    public static void Register()
    {
        re.Events.OnNet<types.roaming.PlayerJoinedPayload>(
            types.roaming.MessageNames.PlayerJoined,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnPlayerJoined(payload);
                }
            }
        );

        re.Events.OnNet<types.roaming.PlayerLeftPayload>(
            types.roaming.MessageNames.PlayerLeft,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnPlayerLeft(payload);
                }
            }
        );

        re.Events.OnNet<types.roaming.MatchBeginPayload>(
            types.roaming.MessageNames.MatchBegin,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnMatchBegin(payload);
                }
            }
        );

        re.Events.OnNet<types.roaming.MatchSyncPayload>(
            types.roaming.MessageNames.MatchSync,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnMatchSync(payload);
                }
            }
        );

        re.Events.OnNet<types.roaming.PlayerStateChangedPayload>(
            types.roaming.MessageNames.PlayerStateChanged,
            async (payload) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnPlayerStateChanged(payload);
                }
            }
        );

        re.Events.OnNet(
            types.roaming.MessageNames.PlayerVehicleSpawned,
            async () =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OnPlayerVehicleSpawned();
                }
            }
        );

        re.Con.RegisterCommandWithKeyBind(
            "roaming:toggle-help-screen",
            (string[] args) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.ToggleHelpScreen();
                }
            },
            EInputKey.IK_6,
            "[Roaming] Open or close the help screen"
        );

        re.Con.RegisterCommandWithKeyBind(
            "roaming:open-customization-screen",
            (string[] args) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.OpenCustomizationScreen();
                }
            },
            EInputKey.IK_7,
            "[Roaming] Open character customization screen"
        );

        re.Con.RegisterCommandWithKeyBind(
            "roaming:spawn-random-vehicle",
            (string[] args) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.SpawnRandomVehicle();
                }
            },
            EInputKey.IK_8,
            "[Roaming] Spawn random vehicle"
        );

        re.Con.RegisterCommandWithKeyBind(
            "roaming:toggle-combat-state",
            (string[] args) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.ToggleCombatState();
                }
            },
            EInputKey.IK_9,
            "[Roaming] Enable or disable combat"
        );

        re.Con.RegisterCommandWithKeyBind(
            "roaming:request-respawn",
            (string[] args) =>
            {
                if (GameModeManager.GetActiveMode()?.GameMode is Modes.Roaming roaming)
                {
                    roaming.RequestRespawn();
                }
            },
            EInputKey.IK_Space,
            "[Roaming] Request respawn"
        );
    }
}
