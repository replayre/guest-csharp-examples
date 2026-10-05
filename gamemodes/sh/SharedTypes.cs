[Serde.GenerateSerde]
public partial record UIMessage<T>(string messageName, T payload)
    where T : Serde.ISerializeProvider<T>, Serde.IDeserializeProvider<T>;

[Serde.GenerateSerde]
public partial record BanListEntry(ulong userid, string? reason);

[Serde.GenerateSerde]
public partial record BanList(BanListEntry[] Entries);