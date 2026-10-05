namespace GameModes.Client;

public interface IGameMode
{
    void OnActivate();
    void OnDeactivate();
    void OnTick();
    void OnEntityAttached(re.EntityAttached evt) { }
    void OnEntityDisposed(re.EntityDisposed evt) { }
    public void OnPuppetHealthChanged(
        PlayerPuppet puppet,
        float newHealth,
        float healthDifference
    ) { }
    public void OnPuppetKilled(PlayerPuppet puppet, game.Object killer) { }
}
