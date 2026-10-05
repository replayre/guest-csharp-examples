public static class ChatHandler
{
    public static void Register()
    {
        re.Events.OnNet(
            types.chat.MessageNames.SendMessage,
            (re.ClientId source, types.chat.SendMessage data) =>
            {
                var username = re.cl.IdentityComponent.GetUsername(source);

                Console.WriteLine($"[Chat] {username}: {data.contents}");

                re.Events.EmitNetBroadcast(
                    types.chat.MessageNames.BroadcastMessage,
                    new types.chat.BroadcastMessage(username, data.contents)
                );
            }
        );
    }
}
