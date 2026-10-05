namespace GameModes.Server;

public static class RaceTrackManager
{
    private static readonly List<types.racing.Track> _tracks = new();
    private static string? _lastPlayedTrackId = null;

    public static types.racing.Track[] GetAllTracks() => _tracks.ToArray();

    public static types.racing.Track? GetTrackById(string id) =>
        _tracks.FirstOrDefault(t => t.id == id);

    public static void LoadTracks()
    {
        _tracks.Clear();

        if (Directory.Exists("tracks"))
        {
            var trackFiles = Directory.EnumerateFiles("tracks", "*.json");
            foreach (var filePath in trackFiles)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var track = Serde.Json.JsonSerializer.Deserialize<types.racing.Track>(json);
                    if (track != null)
                    {
                        _tracks.Add(track);
                        Console.WriteLine(
                            $"[RaceTrackManager] Loaded track from file: {track.name} ({track.id})"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[RaceTrackManager] Error loading track from {filePath}: {ex.Message}"
                    );
                }
            }
        }

        Console.WriteLine($"[RaceTrackManager] Total tracks loaded: {_tracks.Count}");
    }

    public static types.racing.Track? PickRandomTrack()
    {
        if (_tracks.Count == 0)
            return null;

        var candidates = _tracks.Count > 1
            ? _tracks.Where(t => t.id != _lastPlayedTrackId).ToList()
            : _tracks;

        var pick = candidates[Random.Shared.Next(0, candidates.Count)];
        _lastPlayedTrackId = pick.id;
        return pick;
    }
}
