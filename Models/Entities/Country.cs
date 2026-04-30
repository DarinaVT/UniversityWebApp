using Models.Base;
namespace Models.Entities;

public class Country : BaseEntity
{
    public string Name { get; set; }
    public ICollection<City> Cities { get; set; }
    public ICollection<University> Universities { get; set; }
}
