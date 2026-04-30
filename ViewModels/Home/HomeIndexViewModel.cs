namespace UniWebApp.ViewModels.Home;

public class HomeIndexViewModel
{
    public List<HomeUniversityViewModel> FeaturedUniversities { get; set; } = new();
    public List<string> Countries { get; set; } = new();
}

