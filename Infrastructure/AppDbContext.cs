using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
namespace Infrastructure;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<Country> Countries { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<University> Universities { get; set; }
    public DbSet<Faculty> Faculties { get; set; }
    public DbSet<Specialty> Specialties { get; set; }
    public DbSet<UniversityFaculty> UniversityFaculties { get; set; }
    public DbSet<FacultySpecialty> FacultySpecialties { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Favourite> Favourites { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<City>()
            .HasOne(c => c.Country)
            .WithMany(cn => cn.Cities)
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<University>()
            .HasOne(u => u.Country)
            .WithMany(c => c.Universities)
            .HasForeignKey(u => u.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<University>()
            .HasOne(u => u.City)
            .WithMany(c => c.Universities)
            .HasForeignKey(u => u.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<University>()
            .Property(u => u.Rating)
            .HasPrecision(18, 2);

        builder.Entity<University>()
            .Property(u => u.GPARequirement)
            .HasPrecision(18, 2);

        builder.Entity<UniversityFaculty>()
            .HasKey(uf => new { uf.UniversityId, uf.FacultyId });

        builder.Entity<UniversityFaculty>()
            .HasOne(uf => uf.University)
            .WithMany(u => u.UniversityFaculties)
            .HasForeignKey(uf => uf.UniversityId);

        builder.Entity<UniversityFaculty>()
            .HasOne(uf => uf.Faculty)
            .WithMany(f => f.UniversityFaculties)
            .HasForeignKey(uf => uf.FacultyId);

        builder.Entity<FacultySpecialty>()
            .HasKey(fs => new { fs.FacultyId, fs.SpecialtyId });

        builder.Entity<FacultySpecialty>()
            .HasOne(fs => fs.Faculty)
            .WithMany(f => f.FacultySpecialties)
            .HasForeignKey(fs => fs.FacultyId);

        builder.Entity<FacultySpecialty>()
            .HasOne(fs => fs.Specialty)
            .WithMany(s => s.FacultySpecialties)
            .HasForeignKey(fs => fs.SpecialtyId);

        builder.Entity<Favourite>()
            .HasKey(f => new { f.UserId, f.UniversityId });

        builder.Entity<Favourite>()
            .HasOne(f => f.User)
            .WithMany(u => u.Favourites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Favourite>()
            .HasOne(f => f.University)
            .WithMany(u => u.Favourites)
            .HasForeignKey(f => f.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(r => r.University)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UniversityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
