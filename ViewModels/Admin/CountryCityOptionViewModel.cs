namespace UniWebApp.ViewModels.Admin;

public class CountryCityOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TranslatedName { get; set; } = string.Empty;
    public int? CountryId { get; set; }
}

