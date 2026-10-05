namespace GameModes.Client;

public static class GameModeManager
{
    public class GameModeDefinition
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool Hidden { get; init; } = false;
        public string? Description { get; init; }
        public string? Icon { get; init; }
        public Func<IGameMode?>? OnResolveGameMode { get; init; }
        public IGameMode? GameMode;
    }

    private static readonly Dictionary<string, GameModeDefinition> _registeredModes = [];
    private static GameModeDefinition? _activeMode;
    private static int _tickGeneration = 0;
    public static bool DebugMode { get; set; } = false;

    public static void RegisterGameMode(GameModeDefinition mode)
    {
        if (_registeredModes.ContainsKey(mode.Id))
        {
            Console.WriteLine($"[GameModeManager] Game mode already registered: {mode.Id}");
            return;
        }

        _registeredModes[mode.Id] = mode;
        Console.WriteLine($"[GameModeManager] Registered game mode: {mode.Id} ({mode.Name})");
    }

    public static void ActivateGameMode(string gameModeId)
    {
        if (!_registeredModes.TryGetValue(gameModeId, out var mode))
        {
            Console.WriteLine($"[GameModeManager] Game mode not found: {gameModeId}");
            return;
        }

        if (_activeMode != null)
        {
            _activeMode.GameMode?.OnDeactivate();
            Console.WriteLine($"[GameModeManager] Deactivated game mode: {_activeMode.Id}");
        }

        _activeMode = mode;
        if (_activeMode.GameMode != null)
        {
            Console.WriteLine($"[GameModeManager] Nulling existing game mode instance");
            _activeMode.GameMode = null;
        }

        mode.GameMode = mode.OnResolveGameMode?.Invoke();
        mode.GameMode?.OnActivate();
        Console.WriteLine($"[GameModeManager] Activated game mode: {gameModeId}");

        var currentGeneration = ++_tickGeneration;
        Future.Spawn(async () =>
        {
            while (currentGeneration == _tickGeneration)
            {
                _activeMode?.GameMode?.OnTick();
                await Future.Yield();
            }
        });
    }

    public static GameModeDefinition? GetActiveMode()
    {
        return _activeMode;
    }

    public static GameModeDefinition? GetMode(string gameModeId)
    {
        return _registeredModes.TryGetValue(gameModeId, out var mode) ? mode : null;
    }

    public static types.gamemodes.GameModeOption[] GetAllModeOptions()
    {
        return _registeredModes
            .Values.Where(m => !m.Hidden)
            .Select(static m => new types.gamemodes.GameModeOption(
                m.Id,
                m.Name,
                m.Hidden,
                m.Description,
                m.Icon
            ))
            .ToArray();
    }

    public static void OnDeactivate()
    {
        _activeMode?.GameMode?.OnDeactivate();
    }

    public static void OnEntityAttached(re.EntityAttached evt)
    {
        _activeMode?.GameMode?.OnEntityAttached(evt);
    }

    public static void OnEntityDisposed(re.EntityDisposed evt)
    {
        _activeMode?.GameMode?.OnEntityDisposed(evt);
    }

    public static void OnPuppetHealthChanged(
        PlayerPuppet puppet,
        float newHealth,
        float healthDifference
    )
    {
        _activeMode?.GameMode?.OnPuppetHealthChanged(puppet, newHealth, healthDifference);
    }

    public static void OnPuppetKilled(PlayerPuppet puppet, game.Object killer)
    {
        _activeMode?.GameMode?.OnPuppetKilled(puppet, killer);
    }
}
