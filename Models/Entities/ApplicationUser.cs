using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace Models.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<Review> Reviews { get; set; }
    public ICollection<Favourite> Favourites { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ProfilePictureUrl { get; set; }
    public string? DisplayName { get; set; }
}
