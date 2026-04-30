using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Seed.Models;

namespace Seed.Services;

public class FacultySeedService
{
    private readonly AppDbContext _db;

    public FacultySeedService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Faculty>> AssignFacultiesAsync(
        University university,
        IEnumerable<FacultyJsonModel> facultyJsonModels)
    {
        var result = new List<Faculty>();

        foreach (var facultyJson in facultyJsonModels)
        {
            var faculty = await _db.Faculties
                .FirstOrDefaultAsync(f => f.Name == facultyJson.Name);

            if (faculty == null)
            {
                faculty = new Faculty
                {
                    Name = facultyJson.Name
                };

                _db.Faculties.Add(faculty);
            }

            if (!await _db.UniversityFaculties.AnyAsync(uf =>
                uf.UniversityId == university.Id &&
                uf.FacultyId == faculty.Id))
            {
                _db.UniversityFaculties.Add(new UniversityFaculty
                {
                    University = university,
                    Faculty = faculty
                });
            }

            result.Add(faculty);
        }

        return result;
    }
}