namespace types.chat;

[Serde.GenerateSerde, Msgpack.Gen]
public partial record SendMessage(string contents, bool releaseFocus);

[Msgpack.Gen]
public partial record BroadcastMessage(string author, string contents);

[Serde.GenerateSerde]
public partial record HudPayload(bool onlyOpen, string? author, string? contents);

public static class MessageNames
{
    public const string BroadcastMessage = "chat:broadcastMessage";
    public const string SendMessage = "chat:sendMessage";
    public const string PayloadSent = "chat:payloadSent";
    public const string HudUpdate = "chat:hudUpdate";
}
