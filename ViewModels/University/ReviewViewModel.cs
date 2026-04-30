namespace UniWebApp.ViewModels.University;

public class ReviewViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int Stars { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}