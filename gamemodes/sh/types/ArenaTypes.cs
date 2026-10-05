namespace types.arena;

[Serde.GenerateSerde]
[Serde.SerdeTypeOptions(MemberFormat = Serde.MemberFormat.PascalCase)]
public enum SpawnpointType
{
    Anyone,
    BlueTeam,
    RedTeam,
}

[Msgpack.Gen, Serde.GenerateSerde]
public partial record SpawnPoint(
    Vector3 Position,
    Quaternion Orientation,
    SpawnpointType Type = SpawnpointType.Anyone
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record ArenaDefinition(
    string id,
    string name,
    Vector3 center,
    float range,
    SpawnPoint[] Spawnpoints
);
