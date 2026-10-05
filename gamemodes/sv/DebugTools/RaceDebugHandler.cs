using types.racing;

// ReSharper disable once CheckNamespace
namespace GameModes.Server.DebugTools;

public static class RaceDebugHandler
{
    public static void Register()
    {
        re.Events.OnNet<RaceTrackRequestPayload>(
            MessageNames.RaceTracksRequest,
            (clientId, _) =>
            {
                var tracks = RaceTrackManager.GetAllTracks();
                if (tracks.Length == 0)
                    RaceTrackManager.LoadTracks();

                tracks = RaceTrackManager.GetAllTracks();
                re.Events.EmitNetTargeted(
                    MessageNames.RaceTracksResponse,
                    clientId,
                    new RaceTracksResponsePayload(tracks)
                );

                Console.WriteLine($"[RaceDebug] Sent {tracks.Length} track(s) to client {clientId}");
            }
        );

        re.Events.OnNet<SaveGridPositionsPayload>(
            MessageNames.SaveGridPositions,
            (clientId, payload) =>
            {
                SaveGridPositions(payload);
            }
        );

        re.Events.OnNet<SaveTrackPayload>(
            MessageNames.SaveTrack,
            (clientId, payload) =>
            {
                SaveTrack(payload.Track);
            }
        );

        re.Con.RegisterCommand("debug", _ =>
        {
            var enabled = !GameModeManager.DebugMode;
            GameModeManager.DebugMode = enabled;

            if (enabled && GameModeManager.Voting.IsVoteActive())
            {
                GameModeManager.Voting.CancelVote();
                Console.WriteLine("[Debug] Cancelled active vote");
            }

            re.Events.EmitNetBroadcast(
                types.gamemodes.MessageNames.DebugModeUpdate,
                new types.gamemodes.DebugModeUpdate(enabled)
            );

            Console.WriteLine($"[Debug] Debug mode {(enabled ? "ON" : "OFF")} — automatic votes {(enabled ? "suppressed" : "resumed")}");
        });

        re.Con.RegisterCommand("endmode", _ =>
        {
            if (GameModeManager.GetActiveMode() == null)
            {
                Console.WriteLine("[Debug] No active game mode");
                return;
            }

            Console.WriteLine($"[Debug] Ending current game mode");
            GameModeManager.EndGameMode();
        });
    }

    private static void SaveGridPositions(SaveGridPositionsPayload payload)
    {
        var track = RaceTrackManager.GetTrackById(payload.TrackId);
        if (track == null)
        {
            Console.WriteLine($"[RaceDebug] Track '{payload.TrackId}' not found — cannot save grid positions");
            return;
        }

        var updated = track with { gridPositions = payload.Positions };
        var json = Serde.Json.JsonSerializer.Serialize(updated);
        var filePath = Path.Combine("tracks", $"{payload.TrackId}.json");
        File.WriteAllText(filePath, json);

        Console.WriteLine($"[RaceDebug] Saved {payload.Positions.Length} grid positions for track '{payload.TrackId}'");
    }

    private static void SaveTrack(Track track)
    {
        if (!Directory.Exists("tracks"))
            Directory.CreateDirectory("tracks");

        var json = Serde.Json.JsonSerializer.Serialize(track);
        var filePath = Path.Combine("tracks", $"{track.id}.json");
        File.WriteAllText(filePath, json);

        Console.WriteLine($"[RaceDebug] Saved track '{track.name}' ({track.id}) with {track.checkpoints.Length} checkpoints to {filePath}");

        RaceTrackManager.LoadTracks();
    }
}
