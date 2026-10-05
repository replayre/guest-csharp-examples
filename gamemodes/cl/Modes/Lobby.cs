namespace GameModes.Client.Modes;

public class Lobby : IGameMode
{
    private Vector4 _rootLobbyPosition = new Vector4(-2224.1125f, 1734.8925f, 7.0999985f, 0.0f);
    public types.gamemodes.ActiveModeUpdate _modeInfo = new(false, false, "", 0, 0);

    public void OnActivate()
    {
        Console.WriteLine("[Lobby] activated");
        UpdateHUD(true, false);
    }

    public void OnDeactivate()
    {
        Console.WriteLine("[Lobby] deactivated");
        UpdateHUD(false, false);
    }

    public async void OnTick()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        if (GameModeManager.DebugMode)
            return;

        var distance = (player.GetWorldPosition() - _rootLobbyPosition).LengthSquared();
        if (distance >= 7000)
        {
            EntityHelper.Teleport(_rootLobbyPosition, Quaternion.Identity);
        }

        var preventionSystem = ScriptGameInstance
            .GetScriptableSystemsContainer(instance)!
            .Get(PreventionSystem.GetSystemName())!
            .Cast<PreventionSystem>();
        if (preventionSystem != null)
        {
            preventionSystem.TogglePreventionSystem(false);
        }
    }

    public void OnActiveModeUpdate(types.gamemodes.ActiveModeUpdate update)
    {
        _modeInfo = update;
        UpdateHUD(true, _modeInfo.IsGameModeActive);
    }

    private void UpdateHUD(bool inLobbyMode, bool active)
    {
        re.Ui.SendMessage(
            new UIMessage<types.lobby.HudPayload>(
                types.lobby.MessageNames.HudUpdate,
                new types.lobby.HudPayload(
                    IsClientInLobbyMode: inLobbyMode,
                    IsGameModeActive: active,
                    IsGameModeJoinable: _modeInfo.IsGameModeJoinable,
                    CurrentGameMode: _modeInfo.CurrentGameMode,
                    CurrentGameModeRemainingDuration: _modeInfo.CurrentGameModeRemainingDuration,
                    CurrentGameModeNumberOfPlayers: _modeInfo.CurrentGameModeNumberOfPlayers
                )
            )
        );
    }
}
