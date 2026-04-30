using Models.Base;

namespace Models.Entities;

public class City : BaseEntity
{
    public string Name { get; set; }
    public int CountryId { get; set; }
    public Country Country { get; set; }
    public ICollection<University> Universities { get; set; }
}
