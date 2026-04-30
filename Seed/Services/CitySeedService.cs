using Models.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Seed.Services;

public class CitySeedService
{
    private readonly AppDbContext _db;

    public CitySeedService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<City> GetOrCreateAsync(string name, int countryId)
    {
        var city = await _db.Cities
            .FirstOrDefaultAsync(c => c.Name == name && c.CountryId == countryId);

        if (city != null)
            return city;

        city = new City { Name = name, CountryId = countryId };
        _db.Cities.Add(city);
        return city;
    }
}
