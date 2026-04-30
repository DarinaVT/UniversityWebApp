using System.Text.Json;

namespace Seed.Services;

public class WikimediaClient
{
    private readonly HttpClient _http;

    public WikimediaClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> TryGetImageAsync(string title)
    {
        title = Uri.EscapeDataString(title);

        var summaryUrl = $"https://en.wikipedia.org/api/rest_v1/page/summary/{title}";

        try
        {
            var response = await _http.GetAsync(summaryUrl);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("thumbnail", out var thumb) &&
                thumb.TryGetProperty("source", out var src))
            {
                return src.GetString();
            }
        }
        catch { }

        return null;
    }
}