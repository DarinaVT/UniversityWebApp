using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Services;

public class TranslationService : ITranslationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TranslationService> _logger;
    private readonly string _apiUrl;

    public TranslationService(
        IHttpClientFactory httpClientFactory,
        ILogger<TranslationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiUrl = "https://api.mymemory.translated.net/get";
    }

    public async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{_apiUrl}?q={Uri.EscapeDataString(text)}&langpair={fromLanguage}|{toLanguage}";
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Translation API returned status {StatusCode} for text: {Text}", 
                    response.StatusCode, text);
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonResponse);

            if (jsonDoc.RootElement.TryGetProperty("responseData", out var responseData))
            {
                if (responseData.TryGetProperty("translatedText", out var translatedText))
                {
                    var translation = translatedText.GetString();
                    _logger?.LogInformation("Translated '{Text}' from {FromLang} to {ToLang}: '{Translation}'", 
                        text, fromLanguage, toLanguage, translation);
                    return translation;
                }
            }

            _logger?.LogWarning("Translation API response missing translatedText for: {Text}", text);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error translating text '{Text}' from {FromLang} to {ToLang}", 
                text, fromLanguage, toLanguage);
            return null;
        }
    }

    public async Task<string?> DetectLanguageAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en";

        if (ContainsCyrillic(text))
            return "bg";

        if (IsLikelyEnglish(text))
            return "en";

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var testTranslation = await TranslateAsync(text, "auto", "en");
            if (testTranslation != null && testTranslation.ToLower() != text.ToLower())
            {
                return ContainsCyrillic(text) ? "bg" : "en";
            }
        }
        catch
        {
        }

        return "en";
    }

    public async Task<(string englishName, string bulgarianName)> TranslateUniversityNameAsync(string inputName)
    {
        if (string.IsNullOrWhiteSpace(inputName))
            return (inputName ?? "", inputName ?? "");

        var detectedLang = await DetectLanguageAsync(inputName);
        
        if (detectedLang == "bg")
        {
            var englishName = await TranslateAsync(inputName, "bg", "en") ?? inputName;
            return (englishName, inputName);
        }
        else
        {
            var bulgarianName = await TranslateAsync(inputName, "en", "bg") ?? inputName;
            return (inputName, bulgarianName);
        }
    }

    private bool ContainsCyrillic(string text)
    {
        return text.Any(c => c >= 0x0400 && c <= 0x04FF);
    }

    private bool IsLikelyEnglish(string text)
    {
        var asciiCount = text.Count(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == ' ');
        return asciiCount > text.Length * 0.7;
    }
}

