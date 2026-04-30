using Models.Base;

namespace Models.Entities;

public class Specialty : BaseEntity
{
    public string Name { get; set; }
    public ICollection<FacultySpecialty> FacultySpecialties { get; set; }
}
