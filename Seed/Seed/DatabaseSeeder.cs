using EFCore.BulkExtensions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Seed.Helpers;

namespace Seed.Seed;

public class DatabaseSeeder
{
    private readonly AppDbContext _db;

    public DatabaseSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        if (await _db.Universities.AnyAsync())
        {
            Console.WriteLine("Database is already seeded");
            return;
        }

        _db.ChangeTracker.AutoDetectChangesEnabled = false;
        _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var data = JsonLoader.Load();
        Console.WriteLine($"Loaded {data.Count} universities from JSON");

        var countryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cityPairs = new HashSet<(string City, string Country)>();
        var facultyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var specialtyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var seenUniversities = new HashSet<string>();

        foreach (var uni in data)
        {
            var uniKey = $"{uni.Name}|{uni.Country}|{uni.City}".ToLowerInvariant();
            if (!seenUniversities.Add(uniKey))
                continue;

            if (!string.IsNullOrWhiteSpace(uni.Country))
                countryNames.Add(uni.Country.Trim());

            if (!string.IsNullOrWhiteSpace(uni.City) && !string.IsNullOrWhiteSpace(uni.Country))
                cityPairs.Add((uni.City.Trim(), uni.Country.Trim()));

            foreach (var f in uni.Faculties)
            {
                if (!string.IsNullOrWhiteSpace(f.Name))
                    facultyNames.Add(f.Name.Trim());

                foreach (var s in f.Specialties)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        specialtyNames.Add(s.Trim());
                }
            }
        }

        var countriesToInsert = countryNames
            .Select(n => new Country { Name = n })
            .ToList();

        var facultiesToInsert = facultyNames
            .Select(n => new Faculty { Name = n })
            .ToList();

        var specialtiesToInsert = specialtyNames
            .Select(n => new Specialty { Name = n })
            .ToList();

        Console.WriteLine($"Parents collected: Countries={countriesToInsert.Count}, Cities={cityPairs.Count}, Faculties={facultiesToInsert.Count}, Specialties={specialtiesToInsert.Count}");

        var bulkCfg = new BulkConfig
        {
            PreserveInsertOrder = true,
            SetOutputIdentity = true
        };

        await _db.BulkInsertAsync(countriesToInsert, bulkCfg);
        await _db.BulkInsertAsync(facultiesToInsert, bulkCfg);
        await _db.BulkInsertAsync(specialtiesToInsert, bulkCfg);


        var countryIdByName = countriesToInsert
            .ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var facultyIdByName = facultiesToInsert
            .ToDictionary(f => f.Name, f => f.Id, StringComparer.OrdinalIgnoreCase);

        var specialtyIdByName = specialtiesToInsert
            .ToDictionary(s => s.Name, s => s.Id, StringComparer.OrdinalIgnoreCase);


        var citiesToInsert = cityPairs
            .Select(cp => new City
            {
                Name = cp.City,
                CountryId = countryIdByName[cp.Country]
            })
            .ToList();

        await _db.BulkInsertAsync(citiesToInsert, bulkCfg);

        var cityIdByKey = citiesToInsert.ToDictionary(
            c => (City: c.Name, CountryId: c.CountryId),
            c => c.Id);


        var universitiesToInsert = new List<University>(capacity: seenUniversities.Count);
        var uniFacultyToInsert = new List<UniversityFaculty>();
        var facultySpecialtyToInsert = new List<FacultySpecialty>();

        int processed = 0;
        foreach (var uni in data)
        {
            var uniKey = $"{uni.Name}|{uni.Country}|{uni.City}".ToLowerInvariant();
            if (!seenUniversities.Contains(uniKey))
                continue;
        }

        var uniKeysInserted = new HashSet<string>();

        foreach (var uni in data)
        {
            var key = $"{uni.Name}|{uni.Country}|{uni.City}".ToLowerInvariant();
            if (!uniKeysInserted.Add(key))
                continue;

            var countryId = countryIdByName[uni.Country.Trim()];
            var cityId = cityIdByKey[(uni.City.Trim(), countryId)];

            universitiesToInsert.Add(new University
            {
                Name = uni.Name.Trim(),
                CountryId = countryId,
                CityId = cityId,
                Latitude = uni.Latitude,
                Longitude = uni.Longitude,
                Rating = (decimal)uni.Rating,
                AverageGpa = uni.AverageGpa,
                GPARequirement = (decimal)uni.AverageGpa,
                ImageUrl = "/images/university-placeholder.jpg",
                Description = null,
                Website = null, 
                Email = null, 
                Phone = null 
            });

            processed++;
            if (processed % 1000 == 0)
                Console.WriteLine($"Prepared universities: {processed}/{uniKeysInserted.Count}");
        }

        await _db.BulkInsertAsync(universitiesToInsert, bulkCfg);

        var universityIdByKey = universitiesToInsert.ToDictionary(
            u => $"{u.Name}|{u.CountryId}|{u.CityId}".ToLowerInvariant(),
            u => u.Id);

        var seenFacultySpecialty = new HashSet<(int FacultyId, int SpecialtyId)>();
        var seenUniFaculty = new HashSet<(int UniversityId, int FacultyId)>();

        processed = 0;
        foreach (var uni in data)
        {
            var countryId = countryIdByName[uni.Country.Trim()];
            var cityId = cityIdByKey[(uni.City.Trim(), countryId)];
            var uniMapKey = $"{uni.Name.Trim()}|{countryId}|{cityId}".ToLowerInvariant();

            if (!universityIdByKey.TryGetValue(uniMapKey, out var universityId))
                continue;

            foreach (var f in uni.Faculties)
            {
                if (string.IsNullOrWhiteSpace(f.Name))
                    continue;

                var facultyId = facultyIdByName[f.Name.Trim()];

                if (seenUniFaculty.Add((universityId, facultyId)))
                {
                    uniFacultyToInsert.Add(new UniversityFaculty
                    {
                        UniversityId = universityId,
                        FacultyId = facultyId
                    });
                }

                foreach (var s in f.Specialties)
                {
                    if (string.IsNullOrWhiteSpace(s))
                        continue;

                    var specialtyId = specialtyIdByName[s.Trim()];
                    if (seenFacultySpecialty.Add((facultyId, specialtyId)))
                    {
                        facultySpecialtyToInsert.Add(new FacultySpecialty
                        {
                            FacultyId = facultyId,
                            SpecialtyId = specialtyId
                        });
                    }
                }
            }

            processed++;
            if (processed % 1000 == 0)
                Console.WriteLine($"Prepared links: {processed}/{data.Count}");
        }

        await _db.BulkInsertAsync(uniFacultyToInsert);
        await _db.BulkInsertAsync(facultySpecialtyToInsert);

        Console.WriteLine("Seeding completed 🚀");
    }
}
