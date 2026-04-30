using System.Globalization;

namespace Services;

public interface ILocalizationService
{
    string GetLocalizedName(string englishName, string? entityType = null);

    string GetLocalizedName(string englishName);

    bool IsBulgarianCulture();

    CultureInfo GetCurrentCulture();

    bool MatchesSearch(string englishName, string searchTerm);

    void ReloadTranslations();

    Task<bool> AddOrUpdateTranslationAsync(string englishName, string bulgarianName, string entityType);
}

