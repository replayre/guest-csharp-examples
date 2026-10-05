namespace types.voting;

[Msgpack.Gen, Serde.GenerateSerde]
public partial record VoteStartedEvent(string Name, VoteStartedPayload Data);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record VoteCastEvent(string Name, VoteCastPayload Data);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record VoteCompletedEvent(string Name, VoteCompletedPayload Data);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record VoteStartedPayload(
    string VoteId,
    gamemodes.GameModeOption[] GameModes,
    long DurationMs,
    VotingMetadata? Metadata = null
);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record VotingMetadata(string? Title = null, string? Description = null);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record VoteCastPayload(string VoteId, string GameModeId);

[Msgpack.Gen, Serde.GenerateSerde]
public partial record VoteCompletedPayload(string VoteId, string? WinningGameModeId);

public static class MessageNames
{
    public const string VotingStart = "voting:start";
    public const string VotingCast = "voting:cast";
    public const string VotingCompleted = "voting:completed";
}
