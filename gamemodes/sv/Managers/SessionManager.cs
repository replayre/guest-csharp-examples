using GameModes.Server;

public static class SessionManager
{
    private static Dictionary<ulong, string?> _banList = new();
    private static Dictionary<ulong, string> _players = new();

    public static void Register()
    {
        LoadBanList();

        re.Events.On(
            "ClientConnecting",
            (re.ClientConnecting x) =>
            {
                if (_banList.ContainsKey(x.userid))
                {
                    Console.WriteLine(
                        $"[SessionManager] Prevented banned player {x.username}({x.userid}) from joining!"
                    );
                    return;
                }

                _players[x.userid] = x.username;

                if (!re.GameClientRegistry.AcceptConnection(x.connection))
                {
                    Console.WriteLine(
                        $"[SessionManager] Failed to accept player {x.username}({x.userid}). Crashed during connect?"
                    );
                    return;
                }

                Console.WriteLine($"[SessionManager] Accepted player {x.username}({x.userid})");
            }
        );

        re.Events.On(
            "ClientDisconnecting",
            (re.ClientDisconnecting x) =>
            {
                GameModeManager.OnClientDisconnect(x.id);
            }
        );

        re.Events.On(
            "ClientReadyForObserver",
            (re.ClientReadyForObserver x) =>
            {
                var hasObserver = re.cl.ObserverComponent.GetPrimaryObserverEntity(x.id).IsValid();
                if (hasObserver)
                {
                    return;
                }

                var res = re.EntitySystem.Create(
                    new TDBID("Character.Player_Puppet_Base"),
                    re.EntityType.Humanoid,
                    x.id,
                    new Vector3(-2224.1125f, 1734.8925f, 8.0999985f),
                    Quaternion.Identity,
                    async entity =>
                    {
                        if (entity == null)
                        {
                            re.GameClientRegistry.DisconnectClient(
                                x.id,
                                "Failed to create observer entity"
                            );
                            return;
                        }

                        re.cl.ObserverComponent.SetPrimaryObserverEntity(x.id, entity);
                    },
                    new CName(0xEBADA5168620C5FE),
                    new CName(0x49F85AAED016DB90)
                );
            }
        );

        re.Events.On(
            "UgcBundleStopping",
            (re.UgcBundleStopping x) =>
            {
                if (x.name != "gamemodes")
                {
                    return;
                }

                Console.WriteLine($"[SessionManager] Detected self restart!");
                GameModeManager.OnDeactivate();
            }
        );
    }

    public static void BanPlayer(re.ClientId client, string? reason)
    {
        if (!client.IsValid())
        {
            Console.WriteLine($"[SessionManager] Tried to ban invalid ClientId");
            return;
        }

        var username = re.cl.IdentityComponent.GetUsername(client);
        var userid = re.cl.IdentityComponent.GetUserid(client);

        re.GameClientRegistry.DisconnectClient(client, reason ?? "You are banned from this server");

        _banList.Add(userid, reason);
        SaveBanList();

        Console.WriteLine($"[SessionManager] Banned {username}({userid}) for reason {reason}");
    }

    private static void LoadBanList()
    {
        _banList.Clear();

        try
        {
            var json = File.ReadAllText("bans.json");
            var list = Serde.Json.JsonSerializer.Deserialize<BanList>(json);
            if (list != null)
            {
                foreach (var ban in list.Entries)
                {
                    _banList.Add(ban.userid, ban.reason);
                }

                Console.WriteLine(
                    $"[SessionManager] Loaded ban list with {list.Entries.Length} entries"
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SessionManager] Error loading ban list {ex.Message}");
        }
    }

    private static void SaveBanList()
    {
        try
        {
            var aa = new List<BanListEntry>();
            foreach (var ban in _banList)
            {
                aa.Add(new(ban.Key, ban.Value));
            }

            var serializedData = Serde.Json.JsonSerializer.Serialize(new BanList(aa.ToArray()));
            if (serializedData != null)
            {
                File.WriteAllText("bans.json", serializedData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SessionManager] Failed to flush ban list {ex.Message}");
        }
    }
}
