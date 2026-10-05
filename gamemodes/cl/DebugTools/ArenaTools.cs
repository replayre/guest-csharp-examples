public static class ArenaTools
{
    public static types.arena.ArenaDefinition? _arenaInCreation = null;

    public static void Register()
    {
        re.Con.RegisterCommand(
            "arenaTools::beginCreation",
            args =>
            {
                if (_arenaInCreation != null)
                {
                    _arenaInCreation = null;
                }

                if (args.Length < 2)
                {
                    Console.WriteLine("[ArenaTools] Expected id and name parameters");
                    return;
                }

                var instance = ScriptGameInstance.Get();
                var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
                var player = playerSys?.GetLocalPlayerControlledGameObject();
                if (player == null)
                {
                    return;
                }

                _arenaInCreation = new types.arena.ArenaDefinition(
                    args[0],
                    args[1],
                    player.GetWorldPosition().ToVector3(),
                    0.0f,
                    []
                );
                Console.WriteLine(
                    $"[ArenaTools] Started Arena creation with center {_arenaInCreation.center}"
                );
            }
        );

        re.Con.RegisterCommand(
            "arenaTools::expandRange",
            args =>
            {
                if (_arenaInCreation == null)
                {
                    Console.WriteLine("[ArenaTools] Not in creation mode");
                    return;
                }

                var instance = ScriptGameInstance.Get();
                var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
                var player = playerSys?.GetLocalPlayerControlledGameObject();
                if (player == null)
                {
                    return;
                }

                var distance = (
                    player.GetWorldPosition().ToVector3() - _arenaInCreation.center
                ).LengthSquared();

                if (distance > _arenaInCreation.range)
                {
                    _arenaInCreation = _arenaInCreation with { range = distance };
                    Console.WriteLine($"[ArenaTools] Extended range to {distance}");
                }
                else
                {
                    Console.WriteLine("[ArenaTools] Range was already larger");
                }
            }
        );

        re.Con.RegisterCommand(
            "arenaTools::createSpawnpoint",
            args =>
            {
                if (_arenaInCreation == null)
                {
                    Console.WriteLine("[ArenaTools] Not in creation mode");
                    return;
                }

                if (args.Length < 1)
                {
                    Console.WriteLine("[ArenaTools] Expected spawnpoint type parameter");
                    return;
                }

                var instance = ScriptGameInstance.Get();
                var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
                var player = playerSys?.GetLocalPlayerControlledGameObject();
                if (player == null)
                {
                    return;
                }

                var position = player.GetWorldPosition().ToVector3();
                var orientation = player.GetWorldOrientation();
                var type =
                    args[0] == "0"
                        ? types.arena.SpawnpointType.Anyone
                        : (
                            args[0] == "1"
                                ? types.arena.SpawnpointType.BlueTeam
                                : types.arena.SpawnpointType.RedTeam
                        );

                List<types.arena.SpawnPoint> existingSpawnpoints =
                    _arenaInCreation.Spawnpoints.ToList();

                existingSpawnpoints.Add(new types.arena.SpawnPoint(position, orientation, type));

                _arenaInCreation = _arenaInCreation with
                {
                    Spawnpoints = existingSpawnpoints.ToArray(),
                };

                var distance = (position - _arenaInCreation.center).LengthSquared();

                if (distance > _arenaInCreation.range)
                {
                    _arenaInCreation = _arenaInCreation with { range = distance + 5 };
                    Console.WriteLine(
                        $"[ArenaTools] Extended range to fit new spawnpoint to {distance}"
                    );
                }

                Console.WriteLine(
                    $"[ArenaTools] Added new spawnpoint {position} {orientation} of type {type}"
                );
            }
        );

        re.Con.RegisterCommand(
            "arenaTools::endCreation",
            args =>
            {
                if (_arenaInCreation == null)
                {
                    Console.WriteLine("[ArenaTools] Not in creation mode");
                    return;
                }

                var serialized = Serde.Json.JsonSerializer.Serialize(_arenaInCreation);
                Console.WriteLine($"[ArenaTools] Completed Arena: {serialized}");
            }
        );
    }
}
