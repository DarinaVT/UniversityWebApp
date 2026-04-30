namespace Models.Entities;

public class Favourite
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public int UniversityId { get; set; }
    public University University { get; set; }
}