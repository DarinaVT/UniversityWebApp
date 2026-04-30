namespace Seed.Models;

public class UniversityJsonModel
{
    public string Name { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string City { get; set; } = null!;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double Rating { get; set; }
    public double AverageGpa { get; set; }

    public List<FacultyJsonModel> Faculties { get; set; } = new();
}