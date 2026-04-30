using System.ComponentModel.DataAnnotations;

namespace UniWebApp.ViewModels.Admin;

public class AdminUniversityEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public int CityId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public double AverageGpa { get; set; }
}
