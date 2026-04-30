using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;

namespace Services;

public class LocalizationService : ILocalizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalizationService> _logger;
    private Dictionary<string, Dictionary<string, string>>? _translationsByType;
    private readonly object _lock = new object();
    
    private Dictionary<string, Dictionary<string, string>> TranslationsByType
    {
        get
        {
            if (_translationsByType == null)
            {
                lock (_lock)
                {
                    if (_translationsByType == null)
                    {
                        _translationsByType = LoadTranslations();
                        _logger?.LogInformation("TranslationsByType initialized. Total types: {Count}", _translationsByType?.Count ?? 0);
                    }
                }
            }
            return _translationsByType ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private Dictionary<string, Dictionary<string, string>> LoadTranslations()
    {
        var translationsByType = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "country", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) },
            { "city", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) },
            { "university", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) },
            { "faculty", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) },
            { "specialty", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) }
        };

        try
        {
            var possiblePaths = new List<string>
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "translation.json"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "data", "translation.json")
            };

            if (_environment != null)
            {
                possiblePaths.Add(Path.Combine(_environment.ContentRootPath, "wwwroot", "data", "translation.json"));
            }

            string? jsonPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    jsonPath = path;
                    break;
                }
            }

            if (jsonPath != null && File.Exists(jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                    var jsonData = JsonSerializer.Deserialize<JsonElement>(json);
                    
                    if (jsonData.TryGetProperty("countries", out var countries))
                    {
                        foreach (var country in countries.EnumerateObject())
                        {
                            var englishName = country.Name;
                            var bulgarianName = country.Value.GetString() ?? englishName;
                            translationsByType["country"][englishName] = bulgarianName;
                        }
                    }
                    
                    if (jsonData.TryGetProperty("cities", out var cities))
                    {
                        foreach (var city in cities.EnumerateObject())
                        {
                            var englishName = city.Name;
                            var bulgarianName = city.Value.GetString() ?? englishName;
                            translationsByType["city"][englishName] = bulgarianName;
                        }
                    }
                    
                    if (jsonData.TryGetProperty("universities", out var universities))
                    {
                        foreach (var university in universities.EnumerateObject())
                        {
                            var englishName = university.Name;
                            var bulgarianName = university.Value.GetString() ?? englishName;
                            translationsByType["university"][englishName] = bulgarianName;
                        }
                    }
                    
                    if (jsonData.TryGetProperty("faculties", out var faculties))
                    {
                        foreach (var faculty in faculties.EnumerateObject())
                        {
                            var englishName = faculty.Name;
                            var bulgarianName = faculty.Value.GetString() ?? englishName;
                            translationsByType["faculty"][englishName] = bulgarianName;
                        }
                    }
                    
                    if (jsonData.TryGetProperty("specialties", out var specialties))
                    {
                        foreach (var specialty in specialties.EnumerateObject())
                        {
                            var englishName = specialty.Name;
                            var bulgarianName = specialty.Value.GetString() ?? englishName;
                            translationsByType["specialty"][englishName] = bulgarianName;
                        }
                    }

                    var countryCount = translationsByType["country"].Count;
                    var cityCount = translationsByType["city"].Count;
                    var universityCount = translationsByType["university"].Count;
                    var facultyCount = translationsByType["faculty"].Count;
                    var specialtyCount = translationsByType["specialty"].Count;
                    
                    _logger?.LogInformation("Loaded translations from {Path}. Countries: {CountryCount}, Cities: {CityCount}, Universities: {UniversityCount}, Faculties: {FacultyCount}, Specialties: {SpecialtyCount}",
                        jsonPath, countryCount, cityCount, universityCount, facultyCount, specialtyCount);
                    
                    if (countryCount == 0 && cityCount == 0 && universityCount == 0)
                    {
                        _logger?.LogWarning("WARNING: All translation dictionaries are empty! JSON may not have been parsed correctly.");
                    }
                }
                catch (Exception loadEx)
                {
                    _logger?.LogError(loadEx, "Error loading translations from {Path}. Exception: {Message}", jsonPath, loadEx.Message);
                }
            }
            else
            {
                _logger?.LogWarning("translation.json file not found. Searched paths: {Paths}", string.Join(", ", possiblePaths));
            }
        }
        catch (JsonException jsonEx)
        {
            _logger?.LogError(jsonEx, "JSON syntax error in translation.json at line {LineNumber}. Localization will use English names. Please fix the JSON file.", jsonEx.LineNumber);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading translation.json. Localization will use English names.");
        }

        return translationsByType;
    }

    public LocalizationService(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment environment,
        ILogger<LocalizationService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _logger = logger;
    }

    public string GetLocalizedName(string englishName, string? entityType = null)
    {
        if (string.IsNullOrWhiteSpace(englishName))
            return englishName;

        var isBulgarian = IsBulgarianCulture();
        if (!isBulgarian)
            return englishName;

        try
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                entityType = "country"; 
            }

            var normalizedType = entityType.ToLowerInvariant();
            var translationsDict = TranslationsByType;
            
            if (translationsDict == null || translationsDict.Count == 0)
            {
                _logger?.LogWarning("TranslationsByType is null or empty when trying to translate '{EnglishName}'", englishName);
                return englishName;
            }
            
            if (translationsDict.TryGetValue(normalizedType, out var translations))
            {
                if (translations != null && translations.Count > 0)
                {
                    if (translations.TryGetValue(englishName, out var translation))
                        return translation;

                    var match = translations.FirstOrDefault(kvp => 
                        string.Equals(kvp.Key, englishName, StringComparison.OrdinalIgnoreCase));
                    
                    if (match.Key != null)
                        return match.Value;
                }
            }

            foreach (var typeDict in translationsDict.Values)
            {
                if (typeDict != null && typeDict.Count > 0)
                {
                    if (typeDict.TryGetValue(englishName, out var translation))
                        return translation;
                    
                    var match = typeDict.FirstOrDefault(kvp => 
                        string.Equals(kvp.Key, englishName, StringComparison.OrdinalIgnoreCase));
                    
                    if (match.Key != null)
                        return match.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetLocalizedName for '{EnglishName}' with type '{EntityType}'. Exception: {Message}", englishName, entityType, ex.Message);
        }

        return englishName;
    }

    public string GetLocalizedName(string englishName) => GetLocalizedName(englishName, null);

    public bool IsBulgarianCulture()
    {
        var culture = GetCurrentCulture();
        return culture.TwoLetterISOLanguageName == "bg";
    }

    public CultureInfo GetCurrentCulture()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var requestCulture = httpContext.Features.Get<IRequestCultureFeature>();
            if (requestCulture?.RequestCulture?.UICulture != null)
            {
                return requestCulture.RequestCulture.UICulture;
            }
        }
        
        return CultureInfo.CurrentUICulture;
    }

    public bool MatchesSearch(string englishName, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || string.IsNullOrWhiteSpace(englishName))
            return false;

        var searchLower = searchTerm.ToLower();
        var englishLower = englishName.ToLower();

        if (englishLower.Contains(searchLower))
            return true;

        foreach (var typeDict in TranslationsByType.Values)
        {
            if (typeDict.TryGetValue(englishName, out var bulgarianName))
            {
                var bulgarianLower = bulgarianName.ToLower();
                if (bulgarianLower.Contains(searchLower))
                    return true;
            }
            
            var match = typeDict.FirstOrDefault(kvp => 
                string.Equals(kvp.Key, englishName, StringComparison.OrdinalIgnoreCase));
            
            if (match.Key != null)
            {
                var bulgarianLower = match.Value.ToLower();
                if (bulgarianLower.Contains(searchLower))
                    return true;
            }
        }

        return false;
    }

    public void ReloadTranslations()
    {
        lock (_lock)
        {
            _translationsByType = null; 
            _logger?.LogInformation("Translation cache cleared. Translations will be reloaded on next access.");
        }
    }

    public async Task<bool> AddOrUpdateTranslationAsync(string englishName, string bulgarianName, string entityType)
    {
        if (string.IsNullOrWhiteSpace(englishName) || string.IsNullOrWhiteSpace(entityType))
            return false;

        try
        {
            var possiblePaths = new List<string>
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "translation.json"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "data", "translation.json")
            };

            if (_environment != null)
            {
                possiblePaths.Add(Path.Combine(_environment.ContentRootPath, "wwwroot", "data", "translation.json"));
            }

            string? jsonPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    jsonPath = path;
                    break;
                }
            }

            if (jsonPath == null)
            {
                _logger?.LogWarning("translation.json file not found. Cannot add translation.");
                return false;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath, System.Text.Encoding.UTF8);
            var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent) 
                ?? new Dictionary<string, JsonElement>();

            var normalizedType = entityType.ToLowerInvariant();
            var pluralType = normalizedType switch
            {
                "country" => "countries",
                "city" => "cities",
                "university" => "universities",
                "faculty" => "faculties",
                "specialty" => "specialties",
                _ => normalizedType + "s"
            };

            Dictionary<string, string> translations;
            if (jsonDoc.TryGetValue(pluralType, out var existingElement))
            {
                translations = JsonSerializer.Deserialize<Dictionary<string, string>>(existingElement.GetRawText()) 
                    ?? new Dictionary<string, string>();
            }
            else
            {
                translations = new Dictionary<string, string>();
            }

            translations[englishName] = bulgarianName;

            jsonDoc[pluralType] = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(translations)
            );

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var updatedJson = JsonSerializer.Serialize(jsonDoc, options);
            await File.WriteAllTextAsync(jsonPath, updatedJson, System.Text.Encoding.UTF8);

            ReloadTranslations();

            _logger?.LogInformation("Added/updated translation: {EnglishName} -> {BulgarianName} for type {EntityType}", 
                englishName, bulgarianName, entityType);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding/updating translation for {EnglishName} ({EntityType})", englishName, entityType);
            return false;
        }
    }
}

