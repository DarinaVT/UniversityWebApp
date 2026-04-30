namespace UniWebApp.ViewModels.University;

public class UniversityDetailsViewModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Description { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public decimal Rating { get; set; }
    public double AverageGpa { get; set; }

    public List<FacultyViewModel> Faculties { get; set; } = new();
    public List<ReviewViewModel> Reviews { get; set; } = new();
    public List<ReviewViewModel> PendingReviews { get; set; } = new();
    public bool IsFavourite { get; set; }
    public AddReviewViewModel AddReview { get; set; } = new();
}
