using cl.DebugTools;
using GameModes.Client;
using GameModes.Client.Modes;

public static class App
{
    public static void Start()
    {
        // Managers
        //
        SessionManager.Register();
        NativeRaceHudManager.Register();
        NameTagManager.Register();
        RipperDocManager.Register();

        // Handlers
        //
        VotingHandler.Register();
        GameModeHandler.Register();
        ChatHandler.Register();

        RaceHandler.Register();
        DeathMatchHandler.Register();
        RoamingHandler.Register();

        // DebugTools
        //
#if DEBUG
        ArenaTools.Register();
        RacingTool.Register();
        NoClipTools.Register();
#endif

        RegisterGameModes();

        re.Events.EmitNet(types.gamemodes.MessageNames.ClientReady);
    }

    private static void RegisterGameModes()
    {
        foreach (var mode in GameModeDefinitions.All)
        {
            GameModeManager.RegisterGameMode(
                new GameModeManager.GameModeDefinition
                {
                    Id = mode.Id,
                    Name = mode.Name,
                    Hidden = mode.Hidden,
                    Description = mode.Description,
                    Icon = mode.Icon,
                    OnResolveGameMode = () =>
                    {
                        return mode.Id switch
                        {
                            GameModeDefinitions.Ids.Lobby => new Lobby(),
                            GameModeDefinitions.Ids.TeamDeathmatch => new DeathMatch(),
                            GameModeDefinitions.Ids.FreeForAll => new DeathMatch(),
                            GameModeDefinitions.Ids.Racing => new Racing(),
                            GameModeDefinitions.Ids.Roaming => new Roaming(),
                            _ => null,
                        };
                    },
                }
            );
        }
    }
}
