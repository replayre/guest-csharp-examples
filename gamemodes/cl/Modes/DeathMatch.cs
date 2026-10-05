namespace GameModes.Client.Modes;

public class DeathMatch : IGameMode
{
    public enum MatchState
    {
        Uninitialized,
        Starting,
        Countdown,
        Active,
        Ending,
    }

    private double _matchRemainingCountdown = 0.0;
    private double _matchLastCountdownUpdate = 0.0;
    private double _matchRemainingTime = 0.0;
    private double _matchLastTimeUpdate = 0.0;
    private double _arenaViolationTimer = 0.0;
    private double _lastHudUpdateTime = 0.0;
    private MatchState _matchState;
    private List<types.dm.LeaderboardEntry> _leaderboard = new();

    public void OnActivate()
    {
        Console.WriteLine("[DeathMatch] Mode activated - Hunt down your enemies!");
    }

    public void OnDeactivate()
    {
        Console.WriteLine($"[DeathMatch] Mode deactivated");

        _matchState = MatchState.Uninitialized;
        UpdateHUD();

        SessionManager.Revive();

        RemoveEquipment();

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoMovement"),
            0
        );

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoCombat"),
            0
        );

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoDriving"),
            0
        );

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoGrenadeOrGadget"),
            0
        );

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoWorldInteractions"),
            0
        );

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.InfiniteAmmo"),
            0
        );

        ScriptGameInstance
            .GetAttitudeSystem(instance)!
            .SetAttitudeGroupRelationPersistent(
                new CName("player"),
                new CName("player"),
                EAIAttitude.AIA_Friendly
            );
    }

    public void OnTick()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        EntityHelper.ClearMenuGodmodesOnControlledObject();

        var timeNow = TimingHelper.GetTimeSeconds();

        if (_matchState == MatchState.Starting)
        {
            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoMovement"),
                0
            );

            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoCombat"),
                0
            );

            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoDriving"),
                0
            );

            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoGrenadeOrGadget"),
                0
            );

            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoWorldInteractions"),
                0
            );

            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.InfiniteAmmo"),
                0
            );

            ScriptGameInstance
                .GetAttitudeSystem(instance)!
                .SetAttitudeGroupRelationPersistent(
                    new CName("player"),
                    new CName("player"),
                    EAIAttitude.AIA_Hostile
                );
        }
        else if (_matchState == MatchState.Countdown)
        {
            if ((timeNow - _matchLastCountdownUpdate) >= 1.0)
            {
                game.Object.PlaySoundEvent(player, new CName("time_dilation_focused_exit"));

                _matchLastCountdownUpdate = timeNow;
                _matchRemainingCountdown -= 1.0;
                Console.WriteLine($"[DeathMatch] countdown tick! {_matchRemainingCountdown}");
            }

            if (_matchRemainingCountdown <= -1.0)
            {
                StatusEffectHelper.RemoveStatusEffect(
                    player.Downgrade(),
                    new TDBID("GameplayRestriction.NoMovement"),
                    0
                );

                StatusEffectHelper.RemoveStatusEffect(
                    player.Downgrade(),
                    new TDBID("GameplayRestriction.NoCombat"),
                    0
                );

                _matchState = MatchState.Active;
            }
        }
        else if (_matchState == MatchState.Active)
        {
            if (_matchRemainingTime <= 10.0 && (timeNow - _matchLastTimeUpdate) >= 1.0)
            {
                game.Object.PlaySoundEvent(player, new CName("time_dilation_focused_exit"));

                _matchLastTimeUpdate = timeNow;
            }
        }
        else if (_matchState == MatchState.Ending)
        {
            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoCombat"),
                0
            );

            StatusEffectHelper.RemoveStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.InfiniteAmmo"),
                0
            );

            RemoveEquipment();
        }

        if ((timeNow - _lastHudUpdateTime) > 0.5)
        {
            UpdateHUD();
            _lastHudUpdateTime = timeNow;
        }
    }

    public void OnMatchPrepare(types.arena.SpawnPoint spawnPoint)
    {
        Console.WriteLine($"[DeathMatch] OnMatchPrepare");

        _matchState = MatchState.Starting;

        VehicleHelper.ForcePlayerExitVehicle();
        SessionManager.Revive();

        AddEquipment();

        EntityHelper.Teleport(spawnPoint.Position.ToVector4(), spawnPoint.Orientation);

        re.Events.EmitNet(types.dm.MessageNames.ReadyForMatch);

        Console.WriteLine($"[DeathMatch] ReadyForMatch");
    }

    public void OnMatchBegin(types.dm.MatchBeginPayload matchData)
    {
        Console.WriteLine($"[DeathMatch] OnMatchBegin");

        _matchRemainingCountdown = matchData.countdownSeconds;
        _matchRemainingTime = matchData.matchRemainingSeconds;
        _matchState = MatchState.Countdown;
    }

    public void OnMatchSync(types.dm.MatchSyncPayload syncData)
    {
        Console.WriteLine($"[DeathMatch] OnMatchSync");

        _matchRemainingTime = syncData.matchRemainingSeconds;
        _leaderboard = syncData.LeaderboardEntries.ToList();
    }

    public void OnMatchEnding()
    {
        Console.WriteLine($"[DeathMatch] OnMatchEnding");

        _matchState = MatchState.Ending;
    }

    public void OnRespawnRequested(types.arena.SpawnPoint spawnPoint)
    {
        Console.WriteLine($"[DeathMatch] Starting respawn");

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        EntityHelper.Teleport(spawnPoint.Position.ToVector4(), spawnPoint.Orientation);
        SessionManager.Revive();

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoMovement"),
            0
        );

        _arenaViolationTimer = 0.0;

        re.Events.EmitNet(types.dm.MessageNames.RespawnCompleted);

        Console.WriteLine($"[DeathMatch] Revived and teleported");
    }

    public void OnPuppetHealthChanged(PlayerPuppet puppet, float newHealth, float healthDifference)
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        if (player.GetEntityID() == puppet.GetEntityID() && newHealth == 0.0f)
        {
            re.Events.EmitNet(types.dm.MessageNames.PlayerDeath);
        }
    }

    public void OnPuppetKilled(PlayerPuppet puppet, game.Object killer)
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        if (player.GetEntityID() == puppet.GetEntityID())
        {
            var netid = re.EntitySystem.GetNetIdFromEntity(killer);

            Console.WriteLine(
                $"[DM] Reporting kill on {puppet.GetEntityID()} by {killer.GetEntityID()}"
            );

            re.Events.EmitNet(
                types.dm.MessageNames.PlayerKilled,
                new types.dm.PlayerKilledPayload(netid)
            );

            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoMovement"),
                0
            );
        }
    }

    public void OnKillConfirmed()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        game.Object.PlaySoundEvent(player, new CName("w_cyb_strongarms_spy_perk_charge"));
    }

    public void OnClientArenaViolation(types.dm.ArenaBorderViolation violation)
    {
        Console.WriteLine($"[DeathMatch] OnClientArenaViolation {violation.violationTimer}");

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        _arenaViolationTimer = violation.violationTimer;

        game.Object.PlaySoundEvent(player, new CName("dev_surveillance_camera_detect"));
    }

    public void OnClientForceKilled()
    {
        Console.WriteLine($"[DeathMatch] Client will be force killed");

        EntityHelper.KillCurrentlyControlledObject();

        Console.WriteLine($"[DeathMatch] Client force killed");
    }

    private void UpdateHUD()
    {
        re.Ui.SendMessage(
            new UIMessage<types.dm.HudPayload>(
                types.dm.MessageNames.HudUpdate,
                new types.dm.HudPayload(
                    MatchState: (int)_matchState,
                    MatchTypeName: "FFA",
                    CountdownRemainingSeconds: (int)_matchRemainingCountdown,
                    MatchRemainingSeconds: (int)_matchRemainingTime,
                    leaderboard: _leaderboard.ToArray(),
                    OutsideArena: _arenaViolationTimer > 0.0,
                    OutsideArenaRemainingTime: (int)_arenaViolationTimer
                )
            )
        );
    }

    static List<string> weapons = new List<string>
    {
        "Items.Preset_Kenshin_Frank",
        "Items.Preset_Kenshin_Royce",
        "Items.Preset_Lexington_Wilson",
        "Items.Preset_Liberty_Neon",
        "Items.Preset_Liberty_Yorinobu",
        "Items.Preset_Nue_Military",
        "Items.Preset_Katana_Cocktail",
        "Items.Preset_Kukri_Default",
    };

    private void AddEquipment()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var transactionSystem = ScriptGameInstance.GetTransactionSystem(instance)!;
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        foreach (var weapon in weapons)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
            transactionSystem.GiveItemByTDBID(player, tdbId, 1);
        }
    }

    private void RemoveEquipment()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var transactionSystem = ScriptGameInstance.GetTransactionSystem(instance)!;
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        foreach (var weapon in weapons)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
        }
    }
}
