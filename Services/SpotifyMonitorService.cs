using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopAssistant.Services;

public class SpotifyMonitorService
{
    private readonly IWebSocketService _ws;
    private readonly HttpClient _http = new();
    private CancellationTokenSource? _cts;
    private string _lastTitle = string.Empty;

    public SpotifyMonitorService(IWebSocketService ws) => _ws = ws;

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _ = PollAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task PollAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await CheckSpotifyAsync(); } catch { }
            try { await Task.Delay(3000, ct); } catch (OperationCanceledException) { break; }
        }
    }

    private async Task CheckSpotifyAsync()
    {
        string? title = null;
        foreach (var p in Process.GetProcessesByName("Spotify"))
        {
            var t = p.MainWindowTitle;
            if (!string.IsNullOrWhiteSpace(t) && t != "Spotify" && t != "Spotify Premium" && !t.StartsWith("Spotify "))
            {
                title = t;
                break;
            }
        }

        if (title == null || title == _lastTitle) return;
        _lastTitle = title;

        // Spotify window title: "Track Name - Artist Name"
        var parts = title.Split(" - ", 2, StringSplitOptions.TrimEntries);
        var track = parts[0];
        var artist = parts.Length > 1 ? parts[1] : string.Empty;

        var artworkUrl = await FetchArtworkAsync(track, artist);

        var msg = new
        {
            type = "system_stats",
            payload = new
            {
                spotify_track = new
                {
                    title = track,
                    artist,
                    artworkUrl = artworkUrl ?? string.Empty,
                }
            }
        };

        await _ws.SendMessageAsync(JsonSerializer.Serialize(msg));
    }

    private async Task<string?> FetchArtworkAsync(string track, string artist)
    {
        try
        {
            var term = Uri.EscapeDataString($"{track} {artist}");
            var json = await _http.GetStringAsync($"https://itunes.apple.com/search?term={term}&media=music&limit=1");
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() > 0 && results[0].TryGetProperty("artworkUrl100", out var art))
                return art.GetString();
        }
        catch { }
        return null;
    }
}
