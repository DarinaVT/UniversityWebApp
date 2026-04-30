namespace UniWebApp.ViewModels;

public class AdminUserListViewModel
{
    public string Id { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ReviewsCount { get; set; }
    public int FavouritesCount { get; set; }
}
