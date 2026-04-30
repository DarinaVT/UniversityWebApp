namespace UniWebApp.ViewModels.Admin;

public class AdminUniversityListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Rating { get; set; }
}
