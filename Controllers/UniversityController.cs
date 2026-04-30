using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services;
using UniWebApp.ViewModels.University;
using UniWebApp.Extensions;
using UniWebApp.ViewModels;
using System.Text.Json;
using Models.Entities;

namespace UniWebApp.Controllers
{
    public class UniversityController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILocalizationService _localization;

        public UniversityController(AppDbContext db, ILocalizationService localization)
        {
            _db = db;
            _localization = localization;
        }

        public async Task<IActionResult> Details(string slug)
        {
            University? university = null;
            
            if (!string.IsNullOrEmpty(slug))
            {
                university = await _db.Universities
                    .Where(u => u.Slug != null && u.Slug == slug)
                    .FirstOrDefaultAsync();
            }
            
            if (university == null && !string.IsNullOrEmpty(slug) && int.TryParse(slug, out int id))
            {
                university = await _db.Universities
                    .Where(u => u.Id == id)
                    .FirstOrDefaultAsync();
            }

            if (university == null)
                return NotFound();

            var universityId = university.Id;
            var model = await _db.Universities
                .Where(u => u.Id == universityId)
                .Select(u => new UniversityDetailsViewModel
                {
                    Id = u.Id,
                    Slug = u.Slug ?? u.Id.ToString(),
                    Name = _localization.GetLocalizedName(u.Name, "university"),
                    Country = _localization.GetLocalizedName(u.Country.Name, "country"),
                    City = _localization.GetLocalizedName(u.City.Name, "city"),
                    ImageUrl = u.ImageUrl ?? "/images/university-placeholder.jpg",
                    Description = u.Description,
                    Latitude = u.Latitude,
                    Longitude = u.Longitude,
                    Rating = u.Rating,
                    AverageGpa = u.AverageGpa,

                    Faculties = u.UniversityFaculties
                        .Select(uf => new FacultyViewModel
                        {
                            Name = _localization.GetLocalizedName(uf.Faculty.Name, "faculty"),
                            OriginalName = uf.Faculty.Name, // Keep original English name for ID generation
                            Specialties = uf.Faculty.FacultySpecialties
                                .Select(fs => _localization.GetLocalizedName(fs.Specialty.Name, "specialty"))
                                .ToList()
                        })
                        .ToList(),

                    Reviews = u.Reviews
                        .Where(r => r.IsApproved)
                        .Select(r => new ReviewViewModel
                        {
                            Id = r.Id,
                            UserName = r.User.UserName,
                            Stars = r.Rating,
                            Comment = r.Comment,
                            CreatedAt = r.CreatedOn,
                            ProfilePictureUrl = r.User.ProfilePictureUrl
                        })
                        .ToList(),

                    PendingReviews = u.Reviews
                        .Where(r => !r.IsApproved)
                        .Select(r => new ReviewViewModel
                        {
                            Id = r.Id,
                            UserName = r.User.UserName,
                            Stars = r.Rating,
                            Comment = r.Comment,
                            CreatedAt = r.CreatedOn,
                            ProfilePictureUrl = r.User.ProfilePictureUrl
                        })
                        .ToList(),

                    IsFavourite = User.Identity != null && User.Identity.IsAuthenticated &&
                        u.Favourites.Any(f => f.UserId == User.GetUserId()),

                    AddReview = new AddReviewViewModel
                    {
                        UniversityId = u.Id
                    }
                })
                .FirstOrDefaultAsync();

            if (model == null)
                return NotFound();

            return View("~/Views/University/Details.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> CompareChartData(int id)
        {
            var university = await _db.Universities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (university == null)
                return Json(new List<object>());

            var similarUniversities = await _db.Universities
                .AsNoTracking()
                .Where(u => 
                    u.Id != id && 
                    u.CountryId == university.CountryId &&
                    u.Rating > 0)
                .OrderByDescending(u => u.Rating)
                .Take(5)
                .Select(u => new
                {
                    name = _localization.GetLocalizedName(u.Name, "university"),
                    rating = u.Rating,
                    averageGpa = u.AverageGpa
                })
                .ToListAsync();

            var currentUniversity = new
            {
                name = _localization.GetLocalizedName(university.Name, "university"),
                rating = university.Rating,
                averageGpa = university.AverageGpa
            };

            var result = new List<object> { currentUniversity };
            result.AddRange(similarUniversities);

            return Json(result);
        }
    }
}

