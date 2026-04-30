namespace Models.Entities;

public class UniversityFaculty
{
    public int UniversityId { get; set; }
    public University University { get; set; }
    public int FacultyId { get; set; }
    public Faculty Faculty { get; set; }
}