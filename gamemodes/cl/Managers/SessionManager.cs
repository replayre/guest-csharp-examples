using GameModes.Client;

public static class SessionManager
{
    private static bool _shouldRevive = false;

    private static bool _autoDriveEnabled = false;

    public static void Register()
    {
        re.Hooks.Place(
            "DeathDecisionsWithResurrection",
            "ToResurrect;StateContextStateGameScriptInterface",
            (DeathDecisionsWithResurrection_ToResurrect x) =>
            {
                if (_shouldRevive)
                {
                    Console.WriteLine("Resurrecting now!");

                    _shouldRevive = false;
                    return new re.msgpack.Bool(true);
                }

                return new re.msgpack.Bool(false);
            }
        );

        re.Hooks.PlaceVoid(
            "PlayerGameplayRestrictions",
            "PlayerGameplayRestrictions::SendBlockMenuRequest;PlayerPuppetBoolEMenuType",
            (PlayerGameplayRestrictions_SendBlockMenuRequest x) => { }
        );

        re.Hooks.Place(
            "HighLevelTransition",
            "IsDeathMenuBlocked;StateGameScriptInterface",
            (HighLevelTransition_IsDeathMenuBlocked x) =>
            {
                return new re.msgpack.Bool(true);
            }
        );

        re.Hooks.Place(
            "PlayerPuppet",
            "OnGameAttached",
            (PlayerPuppet_OnGameAttached x) =>
            {
                NameTagManager.PuppetAttached(x.self);

                if (!x.self.IsControlledByLocalPeer())
                {
                    return new re.msgpack.Bool(true);
                }

                return re.Hooks.CallOriginal<re.msgpack.Bool>();
            }
        );

        re.Hooks.Place(
            "PlayerPuppet",
            "OnDetach",
            (PlayerPuppet_OnDetach x) =>
            {
                NameTagManager.PuppetDetached(x.self);

                return re.Hooks.CallOriginal<re.msgpack.Bool>();
            }
        );

        re.Hooks.Place(
            "PlayerPuppet",
            "OnHealthUpdateEvent",
            (PlayerPuppet_OnHealthUpdateEvent x) =>
            {
                var result = re.Hooks.CallOriginal<re.msgpack.Bool>();

                if (x.evt != null)
                {
                    GameModeManager.OnPuppetHealthChanged(
                        x.self,
                        x.evt.value,
                        x.evt.healthDifference
                    );
                }

                return result;
            }
        );

        re.Hooks.PlaceVoid(
            "ScriptedPuppet",
            "RewardKiller;GameObjectgameKillTypeBool",
            (ScriptedPuppet_RewardKiller x) =>
            {
                re.Hooks.CallOriginalVoid();

                var killer = x.killer.Upgrade();
                var victim = x.self.Cast<PlayerPuppet>();

                if (killer != null && victim != null)
                {
                    GameModeManager.OnPuppetKilled(victim, killer);
                }
            }
        );

        re.Hooks.PlaceVoid(
            "PlayerDevelopmentSystem",
            "OnPlayerAttach;PlayerAttachRequest",
            (UIInventoryScriptableSystem_OnPlayerAttach x) =>
            {
                re.Hooks.CallOriginalVoid();
                x.self.playerDevelopmentUpdated = false;
            }
        );

        re.Events.On(
            "UgcBundleStopping",
            (re.UgcBundleStopping x) =>
            {
                if (x.name != "gamemodes")
                {
                    return;
                }

                GameModeManager.OnDeactivate();
                NameTagManager.Shutdown();

                re.Ui.SetInputPolicy(false, false, false);

                Console.WriteLine($"Event::UgcBundleStopping: Bundle {x.name} stopping!");
            }
        );

        re.Events.On(
            "EntityAttached",
            (re.EntityAttached evt) =>
            {
                GameModeManager.OnEntityAttached(evt);
            }
        );

        re.Events.On(
            "EntityDisposed",
            (re.EntityDisposed evt) =>
            {
                GameModeManager.OnEntityDisposed(evt);
            }
        );

        re.Hooks.Place(
            "AutoDriveSystem",
            "GetAutodriveAvailable;",
            (AutoDriveSystem_GetAutodriveAvailable x) =>
            {
                return new re.msgpack.Bool(_autoDriveEnabled);
            }
        );

        re.Hooks.PlaceVoid(
            "AutoDriveSystem",
            "OnAutoDriveHitRequest;AutoDriveHitRequest",
            (AutoDriveSystem_OnAutoDriveHitRequest x) => { }
        );

        re.Hooks.PlaceVoid(
            "VehicleComponent",
            "ToggleTargetingSystemForPanzer;PlayerPuppetBool",
            (VehicleComponent_ToggleTargetingSystemForPanzer x) =>
            {
                return;
            }
        );
    }

    public static void Revive()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        if (!player.IsDead())
        {
            return;
        }

        _shouldRevive = true;
    }

    public static void SetAutodriveEnabled(bool state)
    {
        _autoDriveEnabled = state;
    }
}

[Msgpack.Gen]
public readonly partial record struct DeathDecisionsWithResurrection_ToResurrect(
    DeathDecisionsWithResurrection self,
    game.stateMachine.StateContextScript? stateContext,
    game.stateMachine.GameScriptInterface? scriptInterface
);

[Msgpack.Gen]
public readonly partial record struct PlayerGameplayRestrictions_SendBlockMenuRequest(
    WeakHandle<PlayerPuppet> player,
    bool blockMenu,
    EMenuType menuType
);

[Msgpack.Gen]
public readonly partial record struct HighLevelTransition_IsDeathMenuBlocked(
    HighLevelTransition self,
    game.stateMachine.GameScriptInterface? scriptInterface
);

[Msgpack.Gen]
public readonly partial record struct PlayerPuppet_OnGameAttached(PlayerPuppet self);

[Msgpack.Gen]
public readonly partial record struct PlayerPuppet_OnDetach(PlayerPuppet self);

[Msgpack.Gen]
public readonly partial record struct PlayerPuppet_OnHealthUpdateEvent(
    PlayerPuppet self,
    HealthUpdateEvent? evt
);

[Msgpack.Gen]
public readonly partial record struct ScriptedPuppet_RewardKiller(
    ScriptedPuppet self,
    WeakHandle<game.Object> killer,
    game.KillType killType,
    bool isAnyDamageNonlethal
);

[Msgpack.Gen]
public readonly partial record struct UIInventoryScriptableSystem_OnPlayerAttach(
    PlayerDevelopmentSystem self,
    game.PlayerAttachRequest? request
);

[Msgpack.Gen]
public readonly partial record struct AutoDriveSystem_GetAutodriveAvailable(AutoDriveSystem self);

[Msgpack.Gen]
public readonly partial record struct AutoDriveSystem_OnAutoDriveHitRequest(
    AutoDriveSystem self,
    AutoDriveHitRequest? request
);

[Msgpack.Gen]
public readonly partial record struct VehicleComponent_ToggleTargetingSystemForPanzer(
    PlayerDevelopmentSystem self,
    PlayerPuppet? mountedPlayer,
    bool enable
);
