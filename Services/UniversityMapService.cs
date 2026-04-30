using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class UniversityMapService
{
    private readonly AppDbContext _db;

    public UniversityMapService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UniversityPinDto>> GetTopByCountryAsync(string country, string? alt, int take = 6)
    {
        if (string.IsNullOrWhiteSpace(country))
            return new List<UniversityPinDto>();

        country = country.Trim();
        alt = alt?.Trim();

        var query = _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .AsQueryable();

        query = query.Where(u =>
            u.Country.Name.Contains(country) ||
            (!string.IsNullOrWhiteSpace(alt) && u.Country.Name.Contains(alt)) ||
            country.Contains(u.Country.Name)
        );

        return await query
            .OrderByDescending(u => u.Rating)
            .Take(take)
            .Select(u => new UniversityPinDto
            {
                Id = u.Id,
                Name = u.Name,
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                Rating = u.Rating
            })
            .ToListAsync();
    }
}

public class UniversityPinDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal Rating { get; set; }
}

