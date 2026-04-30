namespace UniWebApp.ViewModels.University;

public class FacultyViewModel
{
    public string Name { get; set; }
    public string OriginalName { get; set; } = string.Empty; // Original English name for ID generation
    public IEnumerable<string> Specialties { get; set; } = new List<string>();
}
