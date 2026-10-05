public static class GameModeDefinitions
{
    public static class Ids
    {
        public const string Lobby = "lobby";
        public const string TeamDeathmatch = "tdm";
        public const string FreeForAll = "dm";
        public const string Racing = "racing";
        public const string Roaming = "roaming";
    }

    public static readonly types.gamemodes.GameModeOption[] All =
    [
        new types.gamemodes.GameModeOption(
            Id: Ids.Lobby,
            Name: "Lobby",
            Hidden: true,
            Description: "Lobby base mode",
            Icon: ""
        ),
        new types.gamemodes.GameModeOption(
            Id: Ids.TeamDeathmatch,
            Name: "Team Deathmatch",
            Hidden: true,
            Description: "Classic 4v4 combat",
            Icon: "🎯"
        ),
        new types.gamemodes.GameModeOption(
            Id: Ids.FreeForAll,
            Name: "Free For All",
            Hidden: false,
            Description: "Every player for themselves",
            Icon: "⚔️"
        ),
        new types.gamemodes.GameModeOption(
            Id: Ids.Racing,
            Name: "Racing",
            Hidden: false,
            Description: "Race to the finish!",
            Icon: "🏁"
        ),
        new types.gamemodes.GameModeOption(
            Id: Ids.Roaming,
            Name: "Roaming",
            Hidden: false,
            Description: "Explore the world freely",
            Icon: "🗺️"
        ),
    ];

    public static types.gamemodes.GameModeOption? GetById(string id)
    {
        return All.FirstOrDefault(mode => mode.Id == id);
    }

    public static string[] GetAllIds()
    {
        return All.Select(mode => mode.Id).ToArray();
    }
}
