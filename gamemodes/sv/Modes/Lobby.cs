namespace GameModes.Server.Modes;

public class Lobby() : IGameMode
{
    public void OnActivate()
    {
        Console.WriteLine("[Lobby] OnActivate");
    }

    public void OnDeactivate()
    {
        Console.WriteLine("[Lobby] OnDeactivate");
    }

    public bool OnTick()
    {
        return false;
    }

    public void OnClientDisconnect(re.ClientId client)
    {
        Console.WriteLine($"[Lobby] OnClientDisconnect {client}");
    }

    public double GetRemainingRuntime()
    {
        return 9999.99;
    }

    public int GetNumberOfPlayers()
    {
        return 0;
    }
}
