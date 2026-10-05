namespace types.roaming;

[Msgpack.Gen]
public partial record PlayerJoinedPayload(
    re.ClientId id,
    re.EntityId entity,
    string name,
    bool combatEnabled
);

[Msgpack.Gen]
public partial record PlayerLeftPayload(re.ClientId id);

[Msgpack.Gen]
public partial record MatchBeginPayload(PlayerJoinedPayload[] entries);

[Msgpack.Gen]
public partial record MatchSyncPayload(double matchRemainingSeconds);

[Msgpack.Gen]
public partial record UpdateCombatStatePayload(bool combatState);

[Msgpack.Gen]
public partial record PlayerStateChangedPayload(re.EntityId entity, bool combatEnabled);

[Serde.GenerateSerde]
public partial record HudPayload(
    bool IsActive,
    bool IsDead,
    bool IsHelpScreenEnabled,
    bool IsCharacterCustomizationAvailable,
    bool IsCombatEnabled,
    double TimeUntilRespawn,
    double TimeUntilModeEnds,
    double TimeUntilVehicleSpawnCooldownEnds,
    double TimeUntilCombatToggleCooldownEnds
);

public static class Constants
{
    public const double VEHICLE_SPAWN_COOLDOWN = 10.0;
    public const double COMBAT_TOGGLE_COOLDOWN = 20.0;
}

public static class MessageNames
{
    public const string PlayerJoined = "roaming:playerJoined";
    public const string PlayerLeft = "roaming:playerLeft";
    public const string MatchBegin = "roaming:matchBegin";
    public const string MatchSync = "roaming:matchSync";
    public const string UpdateCombatState = "roaming:updateCombatState";
    public const string PlayerStateChanged = "roaming:playerStateChanged";
    public const string PlayerRequestVehicle = "roaming:playerRequestVehicle";
    public const string PlayerVehicleSpawned = "roaming:playerVehicleSpawned";
    public const string HudUpdate = "roaming:hudUpdate";
}
