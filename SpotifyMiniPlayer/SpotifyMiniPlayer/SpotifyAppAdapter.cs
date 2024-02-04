using System.Diagnostics;
using System.Linq;

namespace SpotifyMiniPlayer;

public class SpotifyAppAdapter
{
    public static string GetCurrentTrack()
    {
        var process = Process.GetProcessesByName("Spotify").FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));

        return process?.MainWindowTitle ?? string.Empty;
    }

    public static bool IsAppRunning()
    {
        var processes = Process.GetProcessesByName("Spotify");

        return processes is not null && processes.Length > 0;
    }
}
