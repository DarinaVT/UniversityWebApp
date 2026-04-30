public class CatalogFilterViewModel
{
    public string? Search { get; set; }

    public int? CountryId { get; set; }
    public int? CityId { get; set; }

    public string? Sort { get; set; }

    public List<KeyValuePair<int, string>> Countries { get; set; } = [];
    public List<KeyValuePair<int, string>> Cities { get; set; } = [];

    public int Page { get; set; } = 1;
}