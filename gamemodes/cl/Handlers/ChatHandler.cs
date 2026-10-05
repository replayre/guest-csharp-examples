public static class ChatHandler
{
    public static void Register()
    {
        re.Ui.SetInputPolicy(accept_input: false, exclusive_input: false, cursor_active: false);

        re.Ui.OnMessage(
            types.chat.MessageNames.PayloadSent,
            (types.chat.SendMessage data) =>
            {
                // Chat UI will send empty contents if input window was closed via 'Esc'
                //
                if (data.contents.Length > 0)
                {
                    re.Events.EmitNet(types.chat.MessageNames.SendMessage, data);
                }

                if (data.releaseFocus)
                {
                    re.Ui.SetInputPolicy(
                        accept_input: false,
                        exclusive_input: false,
                        cursor_active: false
                    );
                }
            }
        );

        re.Events.OnNet(
            types.chat.MessageNames.BroadcastMessage,
            (types.chat.BroadcastMessage data) =>
            {
                re.Ui.SendMessage(
                    new UIMessage<types.chat.HudPayload>(
                        types.chat.MessageNames.HudUpdate,
                        new types.chat.HudPayload(false, data.author, data.contents)
                    )
                );
            }
        );

        re.Con.RegisterCommandWithKeyBind(
            "open-chat-window",
            (string[] args) =>
            {
                re.Ui.SendMessage(
                    new UIMessage<types.chat.HudPayload>(
                        types.chat.MessageNames.HudUpdate,
                        new types.chat.HudPayload(true, null, null)
                    )
                );

                re.Ui.SetInputPolicy(
                    accept_input: true,
                    exclusive_input: true,
                    cursor_active: true
                );
            },
            EInputKey.IK_Enter,
            "Open Chat Window"
        );
    }
}
