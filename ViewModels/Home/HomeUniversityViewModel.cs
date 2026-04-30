namespace UniWebApp.ViewModels.Home;

public class HomeUniversityViewModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    public decimal Rating { get; set; }
    public double AverageGpa { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
