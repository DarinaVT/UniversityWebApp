namespace Seed.Models;

public class FacultyJsonModel
{
    public string Name { get; set; } = null!;
    public List<string> Specialties { get; set; } = new();
}
