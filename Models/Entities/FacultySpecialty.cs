namespace Models.Entities;

public class FacultySpecialty
{
    public int FacultyId { get; set; }
    public Faculty Faculty { get; set; }
    public int SpecialtyId { get; set; }
    public Specialty Specialty { get; set; }
}