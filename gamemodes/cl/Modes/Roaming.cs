namespace GameModes.Client.Modes;

public class Roaming : IGameMode
{
    public class PlayerState
    {
        public re.ClientId Id;
        public re.EntityId Entity;
        public string Name = "";
        public game.NewMappinID? mappinID;
        public bool CombatEnabled;

        public PlayerState(re.ClientId id, re.EntityId entity, string name, bool combatEnabled)
        {
            Id = id;
            Entity = entity;
            Name = name;
            CombatEnabled = combatEnabled;
        }
    }

    private Dictionary<re.ClientId, PlayerState> _playersByClientId = new();
    private Dictionary<re.EntityId, PlayerState> _playersByEntityId = new();
    private double _lastHudUpdateTime = 0.0;
    private double _respawnTime = 0.0;
    private double _remainingMatchTime = 0.0;
    private double _vehicleSpawnCooldownTime = 0.0;
    private double _combatToggleCooldownTime = 0.0;
    private bool _isHelpScreenOpen = true;
    private bool _isCombatEnabled = false;

    public void OnActivate()
    {
        Console.WriteLine("[Roaming] OnActivate");

        FastTravelManager.AddAllFastTravelPoints();
        SessionManager.SetAutodriveEnabled(true);

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        StatusEffectHelper.ApplyStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoGrenadeOrGadget"),
            0
        );

        StatusEffectHelper.ApplyStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.InfiniteAmmo"),
            0
        );

        StatusEffectHelper.ApplyStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoCombat"),
            0
        );

        AddEquipment();
        UnlockSkillTree();

        Future.Spawn(async () =>
        {
            await Future.Sleep(2000);

            TeleportToSpawnPoint();
        });
    }

    public void OnDeactivate()
    {
        Console.WriteLine("[Roaming] OnDeactivate");

        foreach (var entry in _playersByClientId)
        {
            RemovePlayerPinMarker(entry.Value);
            NameTagManager.SetVisible(entry.Value.Entity, false);
        }

        _playersByClientId.Clear();
        _playersByEntityId.Clear();

        FastTravelManager.RemoveAllFastTravelPoints();
        SessionManager.SetAutodriveEnabled(false);

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoGrenadeOrGadget"),
            0
        );

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.InfiniteAmmo"),
            0
        );

        StatusEffectHelper.RemoveStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoCombat"),
            0
        );

        RemoveEquipment();
        ResetSkillTree();

        UpdateHUD(false);
    }

    public void OnTick()
    {
        var timeNow = TimingHelper.GetTimeSeconds();
        if ((timeNow - _lastHudUpdateTime) > 0.5)
        {
            UpdateHUD(true);
            _lastHudUpdateTime = timeNow;
        }

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        var hasEffect = game.StatusEffectSystem.ObjectHasStatusEffect(
            player.Downgrade(),
            new TDBID("GameplayRestriction.NoCombat")
        );
        if (_isCombatEnabled && hasEffect)
        {
            StatusEffectHelper.RemoveStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoCombat"),
                0
            );
        }
        else if (!_isCombatEnabled && !hasEffect)
        {
            StatusEffectHelper.ApplyStatusEffect(
                player.Downgrade(),
                new TDBID("GameplayRestriction.NoCombat"),
                0
            );
        }
    }

    public void OnEntityAttached(re.EntityAttached evt)
    {
        _playersByEntityId.TryGetValue(evt.netid, out var state);
        if (state != null)
        {
            AddPlayerPinMarker(state);
            NameTagManager.SetVisible(state.Entity, true);
            NameTagManager.SetText(state.Entity, state.Name);
            NameTagManager.SetFriendly(state.Entity, !state.CombatEnabled);

            SetEntityCombatState(state.Entity, state.CombatEnabled);
        }
    }

    public void OnEntityDisposed(re.EntityDisposed evt)
    {
        _playersByEntityId.TryGetValue(evt.netid, out var state);
        if (state != null)
        {
            RemovePlayerPinMarker(state);
        }
    }

    public void OnPlayerJoined(types.roaming.PlayerJoinedPayload payload)
    {
        var state = new PlayerState(
            payload.id,
            payload.entity,
            payload.name,
            payload.combatEnabled
        );
        _playersByClientId.Add(payload.id, state);
        _playersByEntityId.Add(payload.entity, state);

        AddPlayerPinMarker(state);

        NameTagManager.SetVisible(state.Entity, true);
        NameTagManager.SetText(state.Entity, state.Name);
        NameTagManager.SetFriendly(state.Entity, !state.CombatEnabled);

        SetEntityCombatState(state.Entity, state.CombatEnabled);
    }

    public void OnPlayerLeft(types.roaming.PlayerLeftPayload payload)
    {
        if (_playersByClientId.Remove(payload.id, out var state))
        {
            _playersByClientId.Remove(state.Id);
            RemovePlayerPinMarker(state);
        }
    }

    public void OnMatchBegin(types.roaming.MatchBeginPayload payload)
    {
        foreach (var entry in payload.entries)
        {
            OnPlayerJoined(entry);
        }
    }

    public void OnMatchSync(types.roaming.MatchSyncPayload syncData)
    {
        _remainingMatchTime = syncData.matchRemainingSeconds;
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
            _respawnTime = TimingHelper.GetTimeSeconds() + 5.0;
        }
    }

    public void OnPlayerStateChanged(types.roaming.PlayerStateChangedPayload payload)
    {
        _playersByEntityId.TryGetValue(payload.entity, out var state);
        if (state != null)
        {
            state.CombatEnabled = payload.combatEnabled;
            NameTagManager.SetFriendly(payload.entity, !state.CombatEnabled);

            SetEntityCombatState(payload.entity, state.CombatEnabled);
        }
    }

    public void SetEntityCombatState(re.EntityId entity, bool combatEnabled)
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;

        var localPuppet = playerSys.GetLocalPlayerControlledGameObject()?.Cast<PlayerPuppet>();
        if (localPuppet == null)
        {
            return;
        }

        var remotePuppet = re
            .EntitySystem.GetEntityFromNetId(entity)
            .Upgrade()
            ?.Cast<ScriptedPuppet>();
        if (remotePuppet != null && localPuppet.GetEntityID() != remotePuppet.GetEntityID())
        {
            var localAgent = localPuppet.GetAttitudeAgent();
            var remoteAgent = remotePuppet.GetAttitudeAgent();

            if (localAgent != null && remoteAgent != null)
            {
                localAgent.SetAttitudeTowards(
                    remoteAgent,
                    combatEnabled ? EAIAttitude.AIA_Neutral : EAIAttitude.AIA_Friendly
                );
            }
        }
    }

    public void OnPlayerVehicleSpawned()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        game.Object.PlaySoundEvent(player, new CName("sq021_sc_10_screen_beep_02"));
    }

    public void ToggleHelpScreen()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject()?.Cast<PlayerPuppet>();
        if (player == null)
        {
            return;
        }

        _isHelpScreenOpen = !_isHelpScreenOpen;

        game.Object.PlaySoundEvent(player, new CName("sq021_sc_10_screen_beep_02"));
    }

    public void OpenCustomizationScreen()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject()?.Cast<PlayerPuppet>();
        if (player == null)
        {
            return;
        }

        if (ink.InkSystem.IsLayerVisible(ink.ELayerType.Menu))
        {
            game.Object.PlaySoundEvent(player, new CName("q303_sc_06b_blocked_action"));
            return;
        }

        var gameInstance = ScriptGameInstance.Get();
        var uiSys = ScriptGameInstance.GetUISystem(gameInstance)!;

        {
            var menuEvent = new ink.MenuInstance_SpawnEvent();
            menuEvent.Init(new("OnOpenPauseMenu"));
            uiSys.QueueEvent(menuEvent);
        }

        {
            var menuInstance = ink.InkSystem.GetMenuInstance()!;
            var userData = new MorphMenuUserData();
            menuInstance.SwitchToScenario(
                new CName("MenuScenario_CharacterCustomizationMirror"),
                userData
            );
        }
    }

    public void SpawnRandomVehicle()
    {
        var timeNow = TimingHelper.GetTimeSeconds();

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject()?.Cast<PlayerPuppet>();
        if (player == null)
        {
            return;
        }

        if (!player.IsDead() && _vehicleSpawnCooldownTime < timeNow)
        {
            _vehicleSpawnCooldownTime = timeNow + types.roaming.Constants.VEHICLE_SPAWN_COOLDOWN;

            re.Events.EmitNet(types.roaming.MessageNames.PlayerRequestVehicle);
        }
        else
        {
            game.Object.PlaySoundEvent(player, new CName("q303_sc_06b_blocked_action"));
        }
    }

    public void ToggleCombatState()
    {
        var timeNow = TimingHelper.GetTimeSeconds();

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        if (_combatToggleCooldownTime > timeNow)
        {
            game.Object.PlaySoundEvent(player, new CName("q303_sc_06b_blocked_action"));
            return;
        }

        _isCombatEnabled = !_isCombatEnabled;
        _combatToggleCooldownTime = timeNow + types.roaming.Constants.COMBAT_TOGGLE_COOLDOWN;

        re.Events.EmitNet(
            types.roaming.MessageNames.UpdateCombatState,
            new types.roaming.UpdateCombatStatePayload(_isCombatEnabled)
        );

        game.Object.PlaySoundEvent(player, new CName("sq021_sc_10_screen_beep_02"));
    }

    public void RequestRespawn()
    {
        var timeNow = TimingHelper.GetTimeSeconds();
        if (timeNow >= _respawnTime)
        {
            SessionManager.Revive();
            _respawnTime = 0.0;
        }
    }

    private void AddPlayerPinMarker(PlayerState state)
    {
        var instance = ScriptGameInstance.Get();
        var mappinSys = ScriptGameInstance.GetMappinSystem(instance)!;
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;

        if (state.mappinID != null)
        {
            return;
        }

        var entity = re.EntitySystem.GetEntityFromNetId(state.Entity);
        if (entity == null)
        {
            return;
        }

        var entityStrong = entity.Upgrade();
        var player = playerSys.GetLocalPlayerControlledGameObject();

        if (entityStrong == null || entityStrong == player)
        {
            return;
        }

        var data = new game.map.pins.MappinData
        {
            mappinType = new TDBID("Mappins.StaticPointOfInterestMappinDefinition"),
            variant = game.data.MappinVariant.HuntForPsychoVariant,
            active = true,
            debugCaption = state.Name,
            visibleThroughWalls = true,
        };

        state.mappinID = mappinSys.RegisterMappinWithObject(
            data,
            entityStrong.Cast<game.Object>()!.Downgrade()
        );
    }

    private void RemovePlayerPinMarker(PlayerState state)
    {
        var instance = ScriptGameInstance.Get();
        var mappinSys = ScriptGameInstance.GetMappinSystem(instance)!;

        if (state.mappinID == null)
        {
            return;
        }

        mappinSys.UnregisterMappin(state.mappinID);

        state.mappinID = null;
    }

    private void UpdateHUD(bool active)
    {
        var timeNow = TimingHelper.GetTimeSeconds();

        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();

        var isDead = player?.Cast<PlayerPuppet>()?.IsDead() == true;

        re.Ui.SendMessage(
            new UIMessage<types.roaming.HudPayload>(
                types.roaming.MessageNames.HudUpdate,
                new types.roaming.HudPayload(
                    IsActive: active,
                    IsDead: isDead,
                    IsHelpScreenEnabled: _isHelpScreenOpen,
                    IsCharacterCustomizationAvailable: !ink.InkSystem.IsLayerVisible(
                        ink.ELayerType.Menu
                    ),
                    IsCombatEnabled: _isCombatEnabled,
                    TimeUntilRespawn: (int)(_respawnTime - timeNow),
                    TimeUntilModeEnds: _remainingMatchTime,
                    TimeUntilVehicleSpawnCooldownEnds: (int)(_vehicleSpawnCooldownTime - timeNow),
                    TimeUntilCombatToggleCooldownEnds: (int)(_combatToggleCooldownTime - timeNow)
                )
            )
        );
    }

    private void UnlockSkillTree()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;

        var localPlayer = playerSys.GetLocalPlayerControlledGameObject()!;
        if (localPlayer == null)
        {
            return;
        }

        game.data.StatType[] attributes =
        [
            game.data.StatType.Reflexes,
            game.data.StatType.TechnicalAbility,
            game.data.StatType.Cool,
            game.data.StatType.Intelligence,
            game.data.StatType.Strength,
        ];

        foreach (var attribute in attributes)
        {
            var reqAttribute = new SetAttribute
            {
                owner = localPlayer.Downgrade(),
                statLevel = 20.0f,
                attributeType = attribute,
            };
            PlayerDevelopmentSystem.GetInstance(localPlayer)?.QueueRequest(reqAttribute);
        }

        for (int i = 0; i < (int)game.data.NewPerkType.Count; i++)
        {
            PlayerDevelopmentSystem
                .GetInstance(localPlayer)
                ?.UnlockNewPerk(localPlayer, (game.data.NewPerkType)i);

            for (int k = 0; k < 3; k++)
            {
                PlayerDevelopmentSystem
                    .GetInstance(localPlayer)
                    ?.GetDevelopmentData(localPlayer)
                    ?.BuyNewPerk((game.data.NewPerkType)i, true);
            }
        }
    }

    private void ResetSkillTree()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;

        var localPlayer = playerSys.GetLocalPlayerControlledGameObject()!;
        if (localPlayer == null)
        {
            return;
        }

        PlayerDevelopmentSystem
            .GetInstance(localPlayer)
            ?.GetDevelopmentData(localPlayer)
            ?.ResetNewPerks();

        PlayerDevelopmentSystem
            .GetInstance(localPlayer)
            ?.GetDevelopmentData(localPlayer)
            ?.ResetAttributes();
    }

    private void TeleportToSpawnPoint()
    {
        var index = Random.Shared.Next(s_spawnpoints.Count());
        var spawnPoint = s_spawnpoints[index];

        EntityHelper.Teleport(spawnPoint.Item1, spawnPoint.Item2);
    }

    static List<(Vector4, Quaternion)> s_spawnpoints = new()
    {
        (
            new Vector4(-1129.4336f, 1481.5616f, 30.247276f, 1),
            new Quaternion(0f, 0f, -0.9998254f, 0.01868718f)
        ),
        (
            new Vector4(-1048.9314f, 1427.0508f, 3.7568893f, 1),
            new Quaternion(0f, 0f, 0.18928953f, -0.9819214f)
        ),
        (
            new Vector4(-860.00037f, 1257.5911f, 28.097557f, 1),
            new Quaternion(0f, 0f, 0.54181814f, 0.8404959f)
        ),
        (
            new Vector4(-1046.2767f, 1311.7197f, 5.243393f, 1),
            new Quaternion(0f, 0f, 0.036234885f, -0.99934334f)
        ),
        (
            new Vector4(-1240.1354f, 1390.28f, 28.293068f, 1),
            new Quaternion(0f, 0f, 0.6728703f, 0.7397606f)
        ),
        (
            new Vector4(-1353.7366f, 1612.1648f, 18.202194f, 1),
            new Quaternion(0f, 0f, 0.37555638f, 0.9267996f)
        ),
        (
            new Vector4(-1600.375f, 1528.9406f, 18.260002f, 1),
            new Quaternion(0f, 0f, 0.8875859f, 0.4606423f)
        ),
        (
            new Vector4(-1957.5631f, 1200.2019f, 10.949997f, 1),
            new Quaternion(0f, 0f, 0.9631843f, 0.2688419f)
        ),
        (
            new Vector4(-1780.5068f, 198.70691f, 16.682281f, 1),
            new Quaternion(0f, 0f, -0.97458625f, 0.2240126f)
        ),
        (
            new Vector4(-2409.8848f, 96.87616f, 12.31456f, 1),
            new Quaternion(0f, 0f, 0.80923533f, -0.5874847f)
        ),
        (
            new Vector4(-2396.8237f, -67.10362f, 8.7428665f, 1),
            new Quaternion(0f, 0f, 0.96753657f, 0.25273106f)
        ),
        (
            new Vector4(-2374.059f, -819.2968f, 12.962418f, 1),
            new Quaternion(0f, 0f, 0.9143897f, 0.4048352f)
        ),
        (
            new Vector4(-2408.5059f, -941.2863f, 8.112625f, 1),
            new Quaternion(0f, 0f, 0.9890034f, -0.14789242f)
        ),
        (
            new Vector4(-2309.0688f, -1098.3695f, 14.030273f, 1),
            new Quaternion(0f, 0f, 0.9992101f, 0.039740473f)
        ),
        (
            new Vector4(-1855.8411f, -1237.0402f, 20.240028f, 1),
            new Quaternion(0f, 0f, -0.26754728f, 0.9635447f)
        ),
        (
            new Vector4(-2191.7107f, -2218.3608f, 11.647926f, 1),
            new Quaternion(0f, 0f, 0.18182153f, 0.98333156f)
        ),
        (
            new Vector4(-1850.6608f, -1950.5916f, 48.520454f, 1),
            new Quaternion(0f, 0f, 0.95587814f, 0.29376352f)
        ),
        (
            new Vector4(-1891.2795f, -1619.0778f, 18.033089f, 1),
            new Quaternion(0f, 0f, 0.52820474f, 0.84911704f)
        ),
        (
            new Vector4(-972.34753f, -1744.8757f, 11.138435f, 1),
            new Quaternion(0f, 0f, -0.99058336f, 0.13691087f)
        ),
        (
            new Vector4(446.256f, -2126.146f, 14.301659f, 1),
            new Quaternion(0f, 0f, -0.9866365f, 0.16293676f)
        ),
        (
            new Vector4(623.8193f, -2157.5618f, 38.160545f, 1),
            new Quaternion(0f, 0f, -0.9999816f, -0.006071635f)
        ),
        (
            new Vector4(765.4464f, -2091.8257f, 172.1988f, 1),
            new Quaternion(0f, 0f, 0.4043916f, 0.91458607f)
        ),
        (
            new Vector4(702.7789f, -988.7451f, 28.828957f, 1),
            new Quaternion(0f, 0f, -0.12384445f, 0.9923017f)
        ),
        (
            new Vector4(687.7877f, -593.73486f, 9.957748f, 1),
            new Quaternion(0f, 0f, 0.85682803f, 0.51560223f)
        ),
        (
            new Vector4(194.67227f, -289.48587f, 8.861023f, 1),
            new Quaternion(0f, 0f, 0.18707334f, 0.98234594f)
        ),
        (
            new Vector4(-161.00528f, -86.188965f, 11.1729965f, 1),
            new Quaternion(0f, 0f, 0.71544516f, 0.69866896f)
        ),
        (
            new Vector4(144.6813f, 547.61444f, 117.28241f, 1),
            new Quaternion(0f, 0f, 0.72095317f, 0.69298387f)
        ),
        (
            new Vector4(-512.61035f, 1607.8042f, 32.84687f, 1),
            new Quaternion(0f, 0f, -0.16449675f, 0.9863777f)
        ),
        (
            new Vector4(-457.2742f, 1407.1995f, 37.287018f, 1),
            new Quaternion(0f, 0f, 0.30848992f, 0.95122766f)
        ),
        (
            new Vector4(-702.5545f, 1268.9368f, 37.926582f, 1),
            new Quaternion(0f, 0f, 0.115798734f, 0.9932728f)
        ),
        (
            new Vector4(-715.83795f, 738.93036f, 32.70897f, 1),
            new Quaternion(0f, 0f, 0.111184195f, 0.99379987f)
        ),
        (
            new Vector4(-581.375f, 438.72354f, 18.603485f, 1),
            new Quaternion(0f, 0f, 0.2837165f, -0.9589082f)
        ),
        (
            new Vector4(-446.1097f, 511.21872f, 30.155884f, 1),
            new Quaternion(0f, 0f, 0.73398733f, -0.67916316f)
        ),
        (
            new Vector4(-989.7656f, 8.324051f, 13.647522f, 1),
            new Quaternion(0f, 0f, 0.85023874f, 0.5263974f)
        ),
        (
            new Vector4(-953.98785f, -43.234123f, 7.419998f, 1),
            new Quaternion(0f, 0f, 0.88566643f, 0.46432203f)
        ),
        (
            new Vector4(-1541.9143f, 308.36182f, 8.190002f, 1),
            new Quaternion(0f, 0f, -0.13546121f, 0.9907827f)
        ),
        (
            new Vector4(-1144.1262f, 127.66585f, 7.351761f, 1),
            new Quaternion(0f, 0f, -0.5552151f, -0.8317068f)
        ),
        (
            new Vector4(-663.78796f, -435.5244f, 8.199997f, 1),
            new Quaternion(0f, 0f, 0.100683965f, 0.9949185f)
        ),
        (
            new Vector4(-1044.2323f, -916.246f, 8.182121f, 1),
            new Quaternion(0f, 0f, 0.46223676f, 0.8867566f)
        ),
        (
            new Vector4(-1308.2014f, -856.63855f, 11.862305f, 1),
            new Quaternion(0f, 0f, 0.4846101f, 0.8747302f)
        ),
        (
            new Vector4(-1652.9591f, -350.49948f, -13.580208f, 1),
            new Quaternion(0f, 0f, -0.122956246f, 0.99241215f)
        ),
        (
            new Vector4(-1141.4509f, 127.80141f, 7.351761f, 1),
            new Quaternion(0f, 0f, -0.51540786f, -0.85694504f)
        ),
    };

    static List<string> s_weapons = new List<string>
    {
        "Items.Preset_Kenshin_Frank",
        "Items.Preset_Kenshin_Royce",
        "Items.Preset_Lexington_Wilson",
        "Items.Preset_Liberty_Neon",
        "Items.Preset_Liberty_Yorinobu",
        "Items.Preset_Nue_Military",
        "Items.Preset_Katana_Cocktail",
        "Items.Preset_Kukri_Default",
        "Items.Preset_Shingen_Neon",
        "Items.Preset_Carnage_Edgerunners",
        "Items.Preset_Sidewinder_Neon",
        "Items.Preset_Nekomata_Neon",
    };

    static List<string> s_quickhacks = new List<string>
    {
        //Combat Quickhacks
        "Items.OverheatLvl4Program",
        "Items.EMPOverloadLvl4Program",
        "Items.ContagionLvl4Program",
        "Items.BrainMeltLvl4Program",
        //Control Quickhacks
        "Items.BlindLvl4Program",
        "Items.DisableCyberwareLvl4Program",
        "Items.LocomotionMalfunctionLvl4Program",
        "Items.WeaponMalfunctionLvl4Program",
        //Covert Quickhacks
        "Items.PingLvl4Program",
        "Items.WhistleLvl4Program",
        "Items.CommsCallInLvl4Program",
        "Items.MemoryWipeLvl4Program",
        "Items.CommsNoiseLvl4Program",
        //Covert Quickhacks
        "Items.MadnessLvl4Program",
        "Items.SuicideLvl4Program",
        "Items.SystemCollapseLvl4Program",
        "Items.GrenadeExplodeLvl4Program",
        "Items.BlackWallProgramLvl4",
    };

    static List<string> s_cyberware = new List<string>
    {
        //Frontal Cortex
        "Items.IconicAdvancedSubdermalCoProcessorLegendaryPlus",
        "Items.AdvancedBioConductorsLegendaryPlus",
        "Items.IconicBioConductorsLegendaryPlus",
        "Items.AdvancedCamilloRamManagerLegendaryPlus",
        "Items.AdvancedExDiskLegendaryPlus",
        "Items.AdvancedKerenziovBoostSystemLegendaryPlus",
        "Items.AdvancedMechatronicCoreLegendaryPlus",
        "Items.AdvancedMemoryBoostLegendaryPlus",
        "Items.AdvancedSubdermalCoProcessorLegendaryPlus",
        "Items.AdvancedTimeBankLegendaryPlus",
        "Items.IconicCamilloRamManagerLegendaryPlus",
        "Items.AdvancedRamUpgradeLegendaryPlus",
        "Items.AdvancedSelfIceLegendaryPlus",
        //Operating System
        "Items.AdvancedArasakaShadowMKVLegendaryPlus",
        "Items.AdvancedBerserkC2MK4Plus",
        "Items.AdvancedBiotechSigmaMKIVLegendaryPlus",
        "Items.CapacityBoosterLegendaryPlus",
        "Items.AdvancedSandevistanC2MK4Plus",
        "Items.AdvancedSandevistanApogeePlus",
        "Items.AdvancedBerserkC4MK5Plus",
        "Items.HauntedCyberdeck_LegendaryPlus",
        "Items.AdvancedSandevistanC4MK5Plus",
        "Items.AdvancedMilitechParalineMKVLegendaryPlus",
        "Items.AdvancedBerserkC1MK4Plus",
        "Items.AdvancedNetwatchNetdriverMKLegendaryPlus",
        "Items.AdvancedSandevistanC3MK5Plus",
        "Items.AdvancedRavenMicrocyberMKIIILegendaryPlus",
        "Items.AdvancedTetratronicRipplerMKVLegendaryPlus",
        "Items.AdvancedBerserkC3MK5Plus",
        "Items.AdvancedSandevistanC1MK4Plus",
        //Arms
        "Items.AdvancedStrongArmsLegendaryPlus",
        "Items.AdvancedStrongArmsElectricLegendaryPlus",
        "Items.AdvancedStrongArmsThermalLegendaryPlus",
        "Items.AdvancedStrongArmsChemicalLegendaryPlus",
        "Items.AdvancedMantisBladesLegendaryPlus",
        "Items.AdvancedMantisBladesElectricLegendaryPlus",
        "Items.AdvancedMantisBladesThermalLegendaryPlus",
        "Items.AdvancedMantisBladesChemicalLegendaryPlus",
        "Items.AdvancedMaxTacMantisBladesLegendaryPlus",
        //"Items.AdvancedProjectileLauncherLegendaryPlus",
        //"Items.AdvancedProjectileLauncherElectricLegendaryPlus",
        //"Items.AdvancedProjectileLauncherThermalLegendaryPlus",
        //"Items.AdvancedProjectileLauncherChemicalLegendaryPlus",
        "Items.AdvancedNanoWiresLegendaryPlus",
        "Items.AdvancedNanoWiresElectricLegendaryPlus",
        "Items.AdvancedNanoWiresThermalLegendaryPlus",
        "Items.AdvancedNanoWiresChemicalLegendaryPlus",
        //Face
        "Items.MaskCW",
        "Items.AdvancedKiroshiOpticsBareLegendaryPlus",
        "Items.AdvancedKiroshiOpticsWallhackLegendaryPlus",
        "Items.Iconic_AdvancedKiroshiOpticsBareLegendaryPlus",
        "Items.AdvancedKiroshiOpticsHunterLegendaryPlus",
        "Items.AdvancedKiroshiOpticsSensorLegendaryPlus",
        "Items.AdvancedKiroshiOpticsPiercingLegendaryPlus",
        "Items.AdvancedKiroshiOpticsCombinedLegendaryPlus",
        //Skeleton
        "Items.AdvancedBionicJointsLegendaryPlus",
        "Items.AdvancedDenseMarrowLegendaryPlus",
        "Items.AdvancedEndoskeletonLegendaryPlus",
        "Items.AdvancedNeuroMatrixLegendaryPlus",
        "Items.AdvancedBoneMarrowCellsLegendaryPlus",
        "Items.AdvancedT1000LegendaryPlus",
        "Items.AdvancedCompilingSkeletonLegendaryPlus",
        "Items.IconicAdvancedT1000LegendaryPlus",
        "Items.AdvancedNoPainNoGainLegendaryPlus",
        "Items.AdvancedRapidMuscleNurishLegendaryPlus",
        "Items.AdvancedAgileJointsLegendaryPlus",
        "Items.AdvancedTitaniumInfusedBonesLegendaryPlus",
        "Items.AdvancedPainDistributorLegendaryPlus",
        //Hands
        "Items.AdvancedPowerGripLegendaryPlus",
        "Items.AdvancedKnifeSharpenerLegendaryPlus",
        "Items.IconicGunStabilizerLegendaryPlus",
        "Items.AdvancedMicroGeneratorLegendaryPlus",
        "Items.AdvancedJointLockLegendaryPlus",
        "Items.AdvancedSmartLinkLegendaryPlus",
        "Items.AdvancedCasiusTattoo",
        "Items.AdvancedSilverhandTattoo",
        "Items.AdvancedYakuzaTattooLegendaryPlus",
        //Nervous System
        "Items.AdvancedDetectorRushLegendaryPlus",
        "Items.IconicAdvancedDetectorRushLegendaryPlus",
        "Items.AdvancedTroubleFinderLegendaryPlus",
        "Items.IconicAdvancedVisualCortexSupportLegendaryPlus",
        "Items.AdvancedKerenzikovLegendaryPlus",
        "Items.AdvancedNeoFiberLegendaryPlus",
        "Items.AdvancedReflexRecorderLegendaryPlus",
        "Items.IconicAdvancedReflexRecorderLegendaryPlus",
        "Items.AdvancedOilDispenserLegendaryPlus",
        "Items.AdvancedSynapticAcceleratorLegendaryPlus",
        "Items.AdvancedTyrosineInjectorLegendaryPlus",
        "Items.AdvancedVisualCortexSupportLegendaryPlus",
        //Circulatory System
        "Items.AdvancedStaminaRegenBoosterLegendaryPlus",
        "Items.AdvancedBiomonitorLegendaryPlus",
        "Items.AdvancedViralVenomLegendaryPlus",
        "Items.AdvancedBloodPumpLegendaryPlus",
        "Items.AdvancedShockAbsorberLegendaryPlus",
        "Items.IconicDischargeConnectorLegendaryPlus",
        "Items.AdvancedDischargeConnectorLegendaryPlus",
        "Items.AdvancedHealOnKillLegendaryPlus",
        "Items.IconicShockAbsorberLegendaryPlus",
        "Items.AdvancedCyberRotorsLegendaryPlus",
        //"Items.AdvancedSecondHeartLegendaryPlus",
        "Items.AdvancedCatchMeIfYouCanLegendaryPlus",
        //Integumentary System
        "Items.AdvancedWeirdTankyPlatingLegendaryPlus",
        "Items.AdvancedAdaptiveStemCellsLegendaryPlus",
        "Items.IconicAdvancedChitonLegendaryPlus",
        "Items.AdvancedCogitoFrameLegendaryPlus",
        "Items.AdvancedSuddenAidLegendaryPlus",
        "Items.AdvancedPlatingGlitchLegendaryPlus",
        "Items.AdvancedNanoTechPlatesLegendaryPlus",
        "Items.AdvancedOpticalCamoLegendaryPlus",
        "Items.AdvancedPainReductorLegendaryPlus",
        "Items.AdvancedBloodDepleterLegendaryPlus",
        "Items.IconicAdvancedProximityReducerLegendaryPlus",
        "Items.AdvancedProximityReducerLegendaryPlus",
        "Items.AdvancedChargeSystemLegendaryPlus",
        "Items.AdvancedElectroshockMechanismLegendaryPlus",
        "Items.AdvancedBoringPlatingLegendaryPlus",
        "Items.AdvancedCatchMeIfYouCanLegendaryPlus",
        "Items.AdvancedCatchMeIfYouCanLegendaryPlus",
        "Items.AdvancedCatchMeIfYouCanLegendaryPlus",
        "Items.AdvancedCatchMeIfYouCanLegendaryPlus",
        "Items.AdvancedCatchMeIfYouCanLegendaryPlus",
        "Items.AdvancedCatchMeIfYouCanLegendaryPlus",
        //Legs
        "Items.AdvancedReinforcedMusclesLegendaryPlus",
        "Items.AdvancedJenkinsTendonsLegendaryPlus",
        "Items.IconicJenkinsTendonsLegendaryPlus",
        "Items.AdvancedCatPawsLegendaryPlus",
        "Items.AdvancedBoostedTendonsLegendaryPlus",
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

        foreach (var weapon in s_weapons)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
            transactionSystem.GiveItemByTDBID(player, tdbId, 1);
        }

        foreach (var weapon in s_quickhacks)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
            transactionSystem.GiveItemByTDBID(player, tdbId, 1);
        }

        foreach (var weapon in s_cyberware)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
            transactionSystem.GiveItemByTDBID(player, tdbId, 1);
        }

        for (int i = 0; i < 50; i++)
        {
            transactionSystem.GiveItemByTDBID(
                player,
                new TDBID("Items.DEBUG_CWCapacityPermaReward_lvl30"),
                1
            );
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

        foreach (var weapon in s_weapons)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
        }

        foreach (var weapon in s_quickhacks)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
        }

        foreach (var weapon in s_cyberware)
        {
            var tdbId = new TDBID(weapon);
            transactionSystem.RemoveItemByTDBID(player, tdbId, 1, true);
        }
    }
}
