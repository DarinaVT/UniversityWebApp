using Models.Base;

namespace Models.Entities;

public class University : BaseEntity
{
    public string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal Rating { get; set; }
    public decimal GPARequirement { get; set; }
    public double AverageGpa { get; set; }
    public int CountryId { get; set; }
    public Country Country { get; set; }
    public int CityId { get; set; }
    public City City { get; set; }
    public ICollection<UniversityFaculty> UniversityFaculties { get; set; } 
    public ICollection<Review> Reviews { get; set; }
    public ICollection<Favourite> Favourites { get; set; }
}