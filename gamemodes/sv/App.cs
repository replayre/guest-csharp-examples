using GameModes.Server;
using GameModes.Server.DebugTools;
using GameModes.Server.Modes;

public static class App
{
    public static void Start()
    {
        // Managers
        //
        SessionManager.Register();

        // Handlers
        //
        VotingHandler.Register();
        GameModeHandler.Register();
        CommandHandler.Register();
        ChatHandler.Register();

        // Debug Handlers
        RaceDebugHandler.Register();

        RaceHandler.Register();
        DeathMatchHandler.Register();
        RoamingHandler.Register();

        RegisterGameModes();
        Console.WriteLine("[App] Game mode system initialized");
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
                            GameModeDefinitions.Ids.FreeForAll => new DeathMatch(
                                DeathMatchFlavor.FFA
                            ),
                            GameModeDefinitions.Ids.Racing => new Racing(),
                            GameModeDefinitions.Ids.Roaming => new Roaming(),
                            _ => null,
                        };
                    },
                }
            );
        }

        GameModeManager.ActivateBaseMode(GameModeDefinitions.Ids.Lobby);

        Console.WriteLine($"[App] Registered {GameModeDefinitions.All.Length} game modes");
    }
}
