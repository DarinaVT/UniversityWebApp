using Infrastructure;
using Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Seed.Services;

public class CountrySeedService
{
    private readonly AppDbContext _db;

    public CountrySeedService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Country> GetOrCreateAsync(string name)
    {
        var country = await _db.Countries.FirstOrDefaultAsync(c => c.Name == name);
        if (country != null)
            return country;

        country = new Country { Name = name };
        _db.Countries.Add(country);
        return country;
    }
}
