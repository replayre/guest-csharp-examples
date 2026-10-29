namespace GameModes.Server;

public interface IGameMode
{
    void OnActivate();
    void OnDeactivate();
    bool OnTick();
    void OnClientDelayedJoin(re.ClientId client) { }
    void OnClientDisconnect(re.ClientId client);
    void OnObserverEntityChanged(re.ClientId client, re.EntityId previous, re.EntityId current) { }
    double GetRemainingRuntime();
    int GetNumberOfPlayers();
    bool IsDelayedJoiningAllowed()
    {
        return false;
    }
}
