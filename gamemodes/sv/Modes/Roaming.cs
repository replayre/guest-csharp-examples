using Microsoft.VisualBasic;

namespace GameModes.Server.Modes;

public class Roaming() : IGameMode
{
    public class PlayerState(re.EntityId entity, string name)
    {
        public re.EntityId Entity = entity;
        public string Name = name;
        public bool CombatEnabled;
        public re.EntityId Vehicle = new();
        public double VehicleSpawnTime = 0.0;
    }

    private Dictionary<re.ClientId, PlayerState> _players = new();
    private const double timeDurationOfMatch = 60.0 * 30;
    private double _matchEndTime = 0.0;
    private double _matchLastSyncTime = 0.0;

    public void OnActivate()
    {
        foreach (var id in re.GameClientRegistry.GetClients())
        {
            _players.Add(
                id,
                new PlayerState(
                    re.cl.ObserverComponent.GetPrimaryObserverEntity(id),
                    re.cl.IdentityComponent.GetUsername(id)
                )
            );
        }

        _matchEndTime = TimingHelper.GetTimeSeconds() + timeDurationOfMatch;

        var players = _players
            .Select(kvp => new types.roaming.PlayerJoinedPayload(
                kvp.Key,
                kvp.Value.Entity,
                kvp.Value.Name,
                kvp.Value.CombatEnabled
            ))
            .ToArray();

        re.Events.EmitNetTargeted(
            types.roaming.MessageNames.MatchBegin,
            _players.Keys.ToList(),
            new types.roaming.MatchBeginPayload(players)
        );
    }

    public void OnDeactivate()
    {
        foreach (var entry in _players)
        {
            if (entry.Value.Vehicle.IsValid())
            {
                re.EntitySystem.Delete(entry.Value.Vehicle);
            }
        }
    }

    public bool OnTick()
    {
        var timeNow = TimingHelper.GetTimeSeconds();

        if (_matchEndTime - timeNow <= 0)
        {
            return true;
        }

        if (_players.Count == 0)
        {
            return true;
        }

        if (timeNow - _matchLastSyncTime >= 1.0)
        {
            re.Events.EmitNetTargeted(
                types.roaming.MessageNames.MatchSync,
                _players.Keys.ToList(),
                new types.roaming.MatchSyncPayload(_matchEndTime - timeNow)
            );

            _matchLastSyncTime = timeNow;
        }

        return false;
    }

    public void OnClientDelayedJoin(re.ClientId client)
    {
        var playerEntity = re.cl.ObserverComponent.GetPrimaryObserverEntity(client);
        var playerName = re.cl.IdentityComponent.GetUsername(client);

        var state = new PlayerState(playerEntity, playerName);
        _players.Add(client, state);

        {
            var allPlayersExceptJoiner = _players.Keys.Where(id => id != client).ToList();

            re.Events.EmitNetTargeted(
                types.roaming.MessageNames.PlayerJoined,
                allPlayersExceptJoiner,
                new types.roaming.PlayerJoinedPayload(client, playerEntity, playerName, false)
            );
        }

        {
            var existingPlayersPseudoJoinedPayloads = _players
                .Select(kvp => new types.roaming.PlayerJoinedPayload(
                    kvp.Key,
                    kvp.Value.Entity,
                    kvp.Value.Name,
                    kvp.Value.CombatEnabled
                ))
                .ToArray();

            re.Events.EmitNetTargeted(
                types.roaming.MessageNames.MatchBegin,
                client,
                new types.roaming.MatchBeginPayload(existingPlayersPseudoJoinedPayloads)
            );
        }
    }

    public void OnClientDisconnect(re.ClientId client)
    {
        re.Events.EmitNetBroadcast(
            types.roaming.MessageNames.PlayerLeft,
            new types.roaming.PlayerLeftPayload(client)
        );

        _players.Remove(client);
    }

    public void OnUpdateCombatState(
        re.ClientId client,
        types.roaming.UpdateCombatStatePayload payload
    )
    {
        _players.TryGetValue(client, out var state);
        if (state != null)
        {
            state.CombatEnabled = payload.combatState;

            re.Events.EmitNetTargeted(
                types.roaming.MessageNames.PlayerStateChanged,
                _players.Keys.ToList(),
                new types.roaming.PlayerStateChangedPayload(state.Entity, state.CombatEnabled)
            );
        }
    }

    public void OnPlayerRequestVehicle(re.ClientId client)
    {
        _players.TryGetValue(client, out var state);
        if (state == null)
        {
            return;
        }

        var timeNow = TimingHelper.GetTimeSeconds();
        if (timeNow - state.VehicleSpawnTime < types.roaming.Constants.VEHICLE_SPAWN_COOLDOWN)
        {
            return;
        }

        if (state.Vehicle.IsValid())
        {
            re.EntitySystem.Delete(state.Vehicle);
        }

        var randomVehicle = VehicleManager.GetRandomVehicle();
        var randomAppearance = VehicleManager.GetRandomAppearanceForVehicle(randomVehicle);
        Console.WriteLine(
            $"[Roaming] Seletect vehicle {randomVehicle} with appearance {randomAppearance} for {client}"
        );

        if (!re.ecs.TransformComponent.Exists(state.Entity))
        {
            return;
        }

        var playerPosition = re.ecs.TransformComponent.GetPosition(state.Entity);
        var playerOrientation = re.ecs.TransformComponent.GetOrientation(state.Entity);

        re.EntitySystem.Create(
            randomVehicle,
            re.EntityType.Vehicle,
            client,
            playerPosition,
            playerOrientation,
            entity =>
            {
                if (entity == null)
                {
                    Console.WriteLine($"[Roaming] Failed to spawn vehicle for client {client}");
                    return;
                }

                Console.WriteLine($"[Roaming] Vehicle spawned for client {client}: {entity}");
                state.Vehicle = entity;

                re.Events.EmitNetTargeted(types.roaming.MessageNames.PlayerVehicleSpawned, client);
            },
            randomAppearance
        );

        state.VehicleSpawnTime = timeNow;
    }

    public double GetRemainingRuntime()
    {
        return _matchEndTime - TimingHelper.GetTimeSeconds();
    }

    public int GetNumberOfPlayers()
    {
        return _players.Count;
    }

    public bool IsDelayedJoiningAllowed()
    {
        var timeNow = TimingHelper.GetTimeSeconds();
        return _matchEndTime - timeNow > 30;
    }
}
