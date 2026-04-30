using System.ComponentModel.DataAnnotations;

namespace UniWebApp.ViewModels.University;

public class AddReviewViewModel
{
    public int UniversityId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000)]
    public string Comment { get; set; }
}