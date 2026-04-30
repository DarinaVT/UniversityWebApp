using Models.Base;

namespace Models.Entities;

public class Faculty : BaseEntity
{
    public string Name { get; set; }
    public ICollection<UniversityFaculty> UniversityFaculties { get; set; }
    public ICollection<FacultySpecialty> FacultySpecialties { get; set; }
}
