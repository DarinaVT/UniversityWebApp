namespace UniWebApp.ViewModels.Catalog;

public class CatalogIndexViewModel
{
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Search { get; set; }
    public string? Sort { get; set; }

    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool IsFavoritesPage { get; set; } = false; 

    public List<CatalogUniversityViewModel> Universities { get; set; } = [];

    public List<string> Countries { get; set; } = [];
    public List<string> Cities { get; set; } = [];
}