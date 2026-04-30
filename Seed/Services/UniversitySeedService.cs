using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Seed.Models;

namespace Seed.Services;

public class UniversitySeedService
{
    private readonly AppDbContext _db;

    public UniversitySeedService(AppDbContext db)
    {
        _db = db;
    }
    public async Task<University> GetOrCreateAsync(
        UniversityJsonModel model,
        int countryId,
        int cityId)
    {
        var existing = await _db.Universities
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Name == model.Name &&
                u.CountryId == countryId);

        if (existing != null)
            return existing;

        var university = new University
        {
            Name = model.Name,
            CountryId = countryId,
            CityId = cityId,

            Rating = (decimal)model.Rating,
            AverageGpa = model.AverageGpa,
            GPARequirement = (decimal)model.AverageGpa, // Set GPARequirement from AverageGpa

            Latitude = model.Latitude,
            Longitude = model.Longitude,

            ImageUrl = "/images/university-placeholder.png",
            Description = null, // Can be populated from JSON if available
            Website = null, // Can be populated from JSON if available
            Email = null, // Can be populated from JSON if available
            Phone = null // Can be populated from JSON if available
        };

        _db.Universities.Add(university);
        return university;
    }
}