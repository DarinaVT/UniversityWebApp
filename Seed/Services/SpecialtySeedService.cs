using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Seed.Models;

namespace Seed.Services;

public class SpecialtySeedService
{
    private readonly AppDbContext _db;

    public SpecialtySeedService(AppDbContext db)
    {
        _db = db;
    }

    public async Task AssignSpecialtiesAsync(
        IEnumerable<Faculty> faculties,
        IEnumerable<FacultyJsonModel> facultyJsonModels)
    {
        foreach (var faculty in faculties)
        {
            var facultyJson = facultyJsonModels
                .FirstOrDefault(f => f.Name == faculty.Name);

            if (facultyJson == null)
                continue;

            foreach (var specialtyName in facultyJson.Specialties)
            {
                var specialty = await _db.Specialties
                    .FirstOrDefaultAsync(s => s.Name == specialtyName);

                if (specialty == null)
                {
                    specialty = new Specialty
                    {
                        Name = specialtyName
                    };

                    _db.Specialties.Add(specialty);
                }

                if (!await _db.FacultySpecialties.AnyAsync(fs =>
                    fs.FacultyId == faculty.Id &&
                    fs.SpecialtyId == specialty.Id))
                {
                    _db.FacultySpecialties.Add(new FacultySpecialty
                    {
                        Faculty = faculty,
                        Specialty = specialty
                    });
                }
            }
        }
    }
}