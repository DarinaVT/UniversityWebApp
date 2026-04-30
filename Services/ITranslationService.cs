namespace Services;

public interface ITranslationService
{
    Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage);
    Task<string?> DetectLanguageAsync(string text);
    Task<(string englishName, string bulgarianName)> TranslateUniversityNameAsync(string inputName);
}

