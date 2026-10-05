namespace GameModes.Server;

public static class ArenaManager
{
    private static readonly List<types.arena.ArenaDefinition> _arenas = new();

    public static types.arena.ArenaDefinition[] GetAllArenas() => _arenas.ToArray();

    public static types.arena.ArenaDefinition? GetArenaById(string id) =>
        _arenas.FirstOrDefault(t => t.id == id);

    public static void LoadArenas()
    {
        _arenas.Clear();

        if (Directory.Exists("arenas"))
        {
            var trackFiles = Directory.EnumerateFiles("arenas", "*.json");
            foreach (var filePath in trackFiles)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var arena = Serde.Json.JsonSerializer.Deserialize<types.arena.ArenaDefinition>(
                        json
                    );
                    if (arena != null)
                    {
                        _arenas.Add(arena);
                        Console.WriteLine(
                            $"[ArenaManager] Loaded arena from file: {arena.name} ({arena.id})"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[ArenaManager] Error loading arena from {filePath}: {ex.Message}"
                    );
                }
            }
        }

        Console.WriteLine($"[ArenaManager] Total arena loaded: {_arenas.Count}");
    }
}
