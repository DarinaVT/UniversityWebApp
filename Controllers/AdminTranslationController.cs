using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Services;

namespace UniWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminTranslationController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AdminTranslationController> _logger;

    public AdminTranslationController(
        AppDbContext db,
        IWebHostEnvironment environment,
        ILocalizationService localizationService,
        ILogger<AdminTranslationController> logger)
    {
        _db = db;
        _environment = environment;
        _localizationService = localizationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> GenerateTranslationsFile()
    {
        try
        {
            var translations = new
            {
                countries = await GetCountriesAsync(),
                cities = await GetCitiesAsync(),
                universities = await GetUniversitiesAsync(),
                faculties = await GetFacultiesAsync(),
                specialties = await GetSpecialtiesAsync()
            };

            var jsonPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot",
                "data",
                "translation.json"
            );

            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var json = JsonSerializer.Serialize(translations, options);
            
            var directory = Path.GetDirectoryName(jsonPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            await System.IO.File.WriteAllTextAsync(jsonPath, json);

            _logger.LogInformation("Generated translation.json with {CountryCount} countries, {CityCount} cities, {UniversityCount} universities, {FacultyCount} faculties, {SpecialtyCount} specialties",
                translations.countries.Count,
                translations.cities.Count,
                translations.universities.Count,
                translations.faculties.Count,
                translations.specialties.Count);

            return Json(new
            {
                success = true,
                message = "Translations file generated successfully",
                counts = new
                {
                    countries = translations.countries.Count,
                    cities = translations.cities.Count,
                    universities = translations.universities.Count,
                    faculties = translations.faculties.Count,
                    specialties = translations.specialties.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating translations file");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult FixTranslationFile()
    {
        try
        {
            var jsonPath = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot",
                "data",
                "translation.json"
            );

            if (!System.IO.File.Exists(jsonPath))
            {
                return Json(new { success = false, message = "translation.json file not found" });
            }

            var content = System.IO.File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
            var fixedContent = FixMalformedTranslations(content);
            
            var backupPath = jsonPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
            System.IO.File.Copy(jsonPath, backupPath);
            
            System.IO.File.WriteAllText(jsonPath, fixedContent, System.Text.Encoding.UTF8);
            
            try
            {
                JsonSerializer.Deserialize<JsonElement>(fixedContent);
            }
            catch (JsonException ex)
            {
                System.IO.File.Copy(backupPath, jsonPath, true);
                return Json(new { success = false, message = $"Fixed JSON is invalid: {ex.Message}. Backup restored." });
            }

            _logger.LogInformation("Fixed translation.json file. Backup saved to {BackupPath}", backupPath);
            
            return Json(new { 
                success = true, 
                message = "Translation file fixed successfully",
                backupPath = backupPath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing translation file");
            return Json(new { success = false, message = ex.Message });
        }
    }

    private string FixMalformedTranslations(string content)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(content);
            return content;
        }
        catch (JsonException)
        {
        }

        var lines = content.Split('\n').ToList();
        var fixedLines = new List<string>();
        var i = 0;
        var fixesCount = 0;

        while (i < lines.Count)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();
            var indent = line.Length - line.TrimStart().Length;
            var indentStr = new string(' ', indent);
            
            var translationMatch = Regex.Match(trimmedLine, @"^""([^""]+)"":\s*""([^""]*)""\s*,?\s*$");
            
            var nullMatch = Regex.Match(trimmedLine, @"^""([^""]+)"":\s*null\s*,?\s*$");
            
            if (translationMatch.Success)
            {
                var key = translationMatch.Groups[1].Value;
                var value = translationMatch.Groups[2].Value;
                var continuationParts = new List<string> { value };
                var j = i + 1;
                var foundContinuation = false;
                
                while (j < lines.Count && j < i + 10)
                {
                    var nextLine = lines[j].Trim();
                    
                    var standaloneString = Regex.Match(nextLine, @"^""([^""]+)""\s*,?\s*$");
                    
                    var nullEntry = Regex.Match(nextLine, @"^""([^""]+)"":\s*null\s*,?\s*$");
                    
                    if (standaloneString.Success)
                    {
                        var contValue = standaloneString.Groups[1].Value;
                        contValue = contValue.Replace("\\\"", "\"").Replace("\"\"", "\"");
                        if (!string.IsNullOrWhiteSpace(contValue))
                        {
                            continuationParts.Add(contValue);
                            foundContinuation = true;
                        }
                        j++;
                    }
                    else if (nullEntry.Success)
                    {
                        var nullKey = nullEntry.Groups[1].Value;
                        nullKey = nullKey.Replace("\\\"", "\"").Replace("\"\"", "\"");
                        if (!string.IsNullOrWhiteSpace(nullKey))
                        {
                            continuationParts.Add(nullKey);
                            foundContinuation = true;
                        }
                        j++;
                    }
                    else
                    {
                        break;
                    }
                }
                
                if (foundContinuation)
                {
                    var combinedValue = string.Join(" ", continuationParts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
                    
                    combinedValue = combinedValue.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                    
                    var fixedLine = $"{indentStr}\"{key}\": \"{combinedValue}\",";
                    fixedLines.Add(fixedLine);
                    
                    i = j;
                    fixesCount++;
                }
                else
                {
                    fixedLines.Add(line);
                    i++;
                }
            }
            else if (nullMatch.Success)
            {
                if (fixedLines.Count > 0)
                {
                    var prevLine = fixedLines[fixedLines.Count - 1].Trim();
                    var prevMatch = Regex.Match(prevLine, @"^""([^""]+)"":\s*""([^""]*)""\s*,?\s*$");
                    
                    if (prevMatch.Success)
                    {
                        var nullKey = nullMatch.Groups[1].Value;
                        nullKey = nullKey.Replace("\\\"", "\"").Replace("\"\"", "\"");
                        
                        fixedLines.RemoveAt(fixedLines.Count - 1);
                        var prevKey = prevMatch.Groups[1].Value;
                        var prevValue = prevMatch.Groups[2].Value;
                        var combined = string.Join(" ", new[] { prevValue, nullKey }.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
                        combined = combined.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
                        
                        var prevIndent = fixedLines.Count > 0 ? new string(' ', fixedLines[fixedLines.Count - 1].Length - fixedLines[fixedLines.Count - 1].TrimStart().Length) : indentStr;
                        fixedLines.Add($"{prevIndent}\"{prevKey}\": \"{combined}\",");
                        fixesCount++;
                        i++;
                        continue;
                    }
                }
                
                _logger.LogWarning("Removing malformed null entry: {Line}", trimmedLine);
                i++;
            }
            else
            {
                fixedLines.Add(line);
                i++;
            }
        }

        var fixedContent = string.Join("\n", fixedLines);
        
        try
        {
            JsonDocument.Parse(fixedContent);
            _logger.LogInformation("Fixed {Count} malformed translation entries. JSON is now valid.", fixesCount);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Fixed JSON still has errors. Fix count: {Count}", fixesCount);
        }
        
        return fixedContent;
    }

    [HttpGet]
    public async Task<IActionResult> PreviewTranslations()
    {
        var translations = new
        {
            countries = await GetCountriesAsync(),
            cities = await GetCitiesAsync(),
            universities = await GetUniversitiesAsync(),
            faculties = await GetFacultiesAsync(),
            specialties = await GetSpecialtiesAsync()
        };

        return Json(translations);
    }

    [HttpPost]
    public IActionResult ReloadTranslations()
    {
        try
        {
            _localizationService.ReloadTranslations();
            _logger.LogInformation("Admin {User} reloaded translations", User.Identity?.Name);
            return Json(new { success = true, message = "Translations reloaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading translations");
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<Dictionary<string, string>> GetCountriesAsync()
    {
        var countries = await _db.Countries
            .Where(c => !c.IsDeleted)
            .Select(c => c.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        var dict = new Dictionary<string, string>();
        foreach (var country in countries)
        {
            dict[country] = country;
        }
        return dict;
    }

    private async Task<Dictionary<string, string>> GetCitiesAsync()
    {
        var cities = await _db.Cities
            .Where(c => !c.IsDeleted)
            .Select(c => c.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        var dict = new Dictionary<string, string>();
        foreach (var city in cities)
        {
            dict[city] = city;
        }
        return dict;
    }

    private async Task<Dictionary<string, string>> GetUniversitiesAsync()
    {
        var universities = await _db.Universities
            .Where(u => !u.IsDeleted)
            .Select(u => u.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        var dict = new Dictionary<string, string>();
        foreach (var university in universities)
        {
            dict[university] = university;
        }
        return dict;
    }

    private async Task<Dictionary<string, string>> GetFacultiesAsync()
    {
        var faculties = await _db.Faculties
            .Where(f => !f.IsDeleted)
            .Select(f => f.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        var dict = new Dictionary<string, string>();
        foreach (var faculty in faculties)
        {
            dict[faculty] = faculty;
        }
        return dict;
    }

    private async Task<Dictionary<string, string>> GetSpecialtiesAsync()
    {
        var specialties = await _db.Specialties
            .Where(s => !s.IsDeleted)
            .Select(s => s.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        var dict = new Dictionary<string, string>();
        foreach (var specialty in specialties)
        {
            dict[specialty] = specialty;
        }
        return dict;
    }
}
