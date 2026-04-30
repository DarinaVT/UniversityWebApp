using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using UniWebApp.Extensions;
using Services;
using UniWebApp.ViewModels.Catalog;
using UniWebApp.ViewModels.University;

namespace UniWebApp.Controllers;

public class UniversitiesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILocalizationService _localization;
    private const int PageSize = 12;

    public UniversitiesController(AppDbContext db, ILocalizationService localization)
    {
        _db = db;
        _localization = localization;
    }

    public async Task<IActionResult> Index(
        string? country,
        string? city,
        string? search,
        string? sort,
        int page = 1)
    {
        var query = _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .Include(u => u.City)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(country))
        {
            var countryLower = country.ToLower();
            
            var englishMatches = await _db.Countries
                .Where(c => 
                    c.Name.ToLower().Contains(countryLower) ||
                    EF.Functions.Like(c.Name, $"%{country}%"))
                .Select(c => c.Id)
                .ToListAsync();
            
            var allCountries = await _db.Countries.ToListAsync();
            var bulgarianMatches = allCountries
                .Where(c => 
                    _localization.MatchesSearch(c.Name, country) ||
                    _localization.GetLocalizedName(c.Name, "country").ToLower().Contains(countryLower))
                .Select(c => c.Id)
                .ToList();
            
            var matchingCountryIds = englishMatches.Union(bulgarianMatches).Distinct().ToList();

            if (matchingCountryIds.Any())
            {
                query = query.Where(u => matchingCountryIds.Contains(u.CountryId));
            }
            else
            {
                query = query.Where(u => false);
            }
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityLower = city.ToLower();
            
            var englishMatches = await _db.Cities
                .Where(c => 
                    c.Name.ToLower().Contains(cityLower) ||
                    EF.Functions.Like(c.Name, $"%{city}%"))
                .Select(c => c.Id)
                .ToListAsync();
            
            var allCities = await _db.Cities.ToListAsync();
            var bulgarianMatches = allCities
                .Where(c => 
                    _localization.MatchesSearch(c.Name, city) ||
                    _localization.GetLocalizedName(c.Name, "city").ToLower().Contains(cityLower))
                .Select(c => c.Id)
                .ToList();
            
            var matchingCityIds = englishMatches.Union(bulgarianMatches).Distinct().ToList();

            if (matchingCityIds.Any())
            {
                query = query.Where(u => matchingCityIds.Contains(u.CityId));
            }
            else
            {
                query = query.Where(u => false);
            }
        }
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            
            var baseQuery = _db.Universities
                .AsNoTracking()
                .Include(u => u.Country)
                .Include(u => u.City)
                .AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(country))
            {
                var countryLower = country.ToLower();
                var matchingCountryIds = await _db.Countries
                    .Where(c => 
                        c.Name.ToLower().Contains(countryLower) ||
                        EF.Functions.Like(c.Name, $"%{country}%"))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (matchingCountryIds.Any())
                {
                    baseQuery = baseQuery.Where(u => matchingCountryIds.Contains(u.CountryId));
                }
                else
                {
                    baseQuery = baseQuery.Where(u => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityLower = city.ToLower();
                var matchingCityIds = await _db.Cities
                    .Where(c => 
                        c.Name.ToLower().Contains(cityLower) ||
                        EF.Functions.Like(c.Name, $"%{city}%"))
                    .Select(c => c.Id)
                    .ToListAsync();

                if (matchingCityIds.Any())
                {
                    baseQuery = baseQuery.Where(u => matchingCityIds.Contains(u.CityId));
                }
                else
                {
                    baseQuery = baseQuery.Where(u => false);
                }
            }
            
            var englishMatches = await baseQuery
                .Where(u => 
                    u.Name.ToLower().Contains(searchLower) ||
                    u.Country.Name.ToLower().Contains(searchLower) ||
                    u.City.Name.ToLower().Contains(searchLower))
                .Select(u => u.Id)
                .ToListAsync();
            
            var allUniversities = await baseQuery.ToListAsync();
            var bulgarianMatches = allUniversities
                .Where(u => 
                    _localization.MatchesSearch(u.Name, search) ||
                    _localization.MatchesSearch(u.Country.Name, search) ||
                    _localization.MatchesSearch(u.City.Name, search))
                .Select(u => u.Id)
                .ToList();
            
            var matchingIds = englishMatches.Union(bulgarianMatches).Distinct().ToList();
            
            if (matchingIds.Any())
            {
                query = query.Where(u => matchingIds.Contains(u.Id));
            }
            else
            {
                query = query.Where(u => false);
            }
        }

        var totalCount = await query.CountAsync();

        var sortedQuery = (sort ?? "rating-desc") switch
        {
            "name-asc" => query.OrderBy(u => u.Name),
            "name-desc" => query.OrderByDescending(u => u.Name),
            "rating-desc" => query.OrderByDescending(u => u.Rating),
            "rating" => query.OrderByDescending(u => u.Rating), 
            "rating-asc" => query.OrderBy(u => u.Rating),
            "gpa-desc" => query.OrderByDescending(u => u.AverageGpa),
            "gpa" => query.OrderByDescending(u => u.AverageGpa), 
            "gpa-asc" => query.OrderBy(u => u.AverageGpa),
            _ => query.OrderByDescending(u => u.Rating)
        };

        var universities = await sortedQuery
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new CatalogUniversityViewModel
            {
                Id = u.Id,
                Name = _localization.GetLocalizedName(u.Name, "university"),
                ImageUrl = u.ImageUrl,
                Rating = u.Rating,
                AverageGpa = u.AverageGpa,
                Country = _localization.GetLocalizedName(u.Country.Name, "country"),
                City = _localization.GetLocalizedName(u.City.Name, "city")
            })
            .ToListAsync();

        var vm = new CatalogIndexViewModel
        {
            Country = country,
            City = city,
            Search = search,
            Sort = sort,
            Page = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
            TotalCount = totalCount,
            Universities = universities,
            Countries = (await _db.Countries
                .OrderBy(c => c.Name)
                .ToListAsync())
                .Select(c => _localization.GetLocalizedName(c.Name, "country"))
                .OrderBy(c => c)
                .ToList(),
            Cities = string.IsNullOrEmpty(country)
                ? []
                : (await _db.Cities
                    .Include(c => c.Country)
                    .OrderBy(c => c.Name)
                    .ToListAsync())
                    .Where(c => 
                        c.Country.Name == country || 
                        (_localization.IsBulgarianCulture() && _localization.GetLocalizedName(c.Country.Name, "country") == country))
                    .Select(c => _localization.GetLocalizedName(c.Name, "city"))
                    .OrderBy(c => c)
                    .ToList()
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_Results", vm);
        }

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var university = await _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .Include(u => u.City)
            .Include(u => u.UniversityFaculties)
                .ThenInclude(uf => uf.Faculty)
                    .ThenInclude(f => f.FacultySpecialties)
                        .ThenInclude(fs => fs.Specialty)
            .Include(u => u.Reviews)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (university == null)
            return NotFound();

        var model = new UniversityDetailsViewModel
        {
            Id = university.Id,
            Name = _localization.GetLocalizedName(university.Name, "university"),
            Country = _localization.GetLocalizedName(university.Country.Name, "country"),
            City = _localization.GetLocalizedName(university.City.Name, "city"),
            ImageUrl = university.ImageUrl,
            Latitude = university.Latitude,
            Longitude = university.Longitude,
            Rating = university.Rating,
            AverageGpa = university.AverageGpa,

            Faculties = university.UniversityFaculties
                .Select(uf => new FacultyViewModel
                {
                    Name = _localization.GetLocalizedName(uf.Faculty.Name, "faculty"),
                    OriginalName = uf.Faculty.Name, // Keep original English name for ID generation
                    Specialties = uf.Faculty.FacultySpecialties
                        .Select(fs => _localization.GetLocalizedName(fs.Specialty.Name, "specialty"))
                        .ToList()
                })
                .ToList(),

            Reviews = university.Reviews
                .Where(r => r.IsApproved)
                .Select(r => new ReviewViewModel
                {
                    UserName = r.User.UserName,
                    Stars = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedOn
                })
                .ToList(),

            IsFavourite = User.Identity.IsAuthenticated &&
                university.Favourites.Any(f => f.UserId == User.GetUserId()),

            AddReview = new AddReviewViewModel
            {
                UniversityId = university.Id
            }
        };

        return View("~/Views/University/Details.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? name, string? country, string? city)
    {
        var query = _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .Include(u => u.City)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(u => u.Name.Contains(name));

        if (!string.IsNullOrWhiteSpace(country))
            query = query.Where(u => u.Country.Name == country);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(u => u.City.Name == city);

        var universities = (await query
            .OrderByDescending(u => u.Rating)
            .ToListAsync())
            .Select(u => new CatalogUniversityViewModel
            {
                Id = u.Id,
                Slug = (u.Slug == null || string.IsNullOrEmpty(u.Slug)) ? u.Id.ToString() : u.Slug,
                Name = _localization.GetLocalizedName(u.Name, "university"),
                ImageUrl = u.ImageUrl,
                Rating = u.Rating,
                AverageGpa = u.AverageGpa,
                Country = _localization.GetLocalizedName(u.Country.Name, "country"),
                City = _localization.GetLocalizedName(u.City.Name, "city")
            })
            .ToList();

        var vm = new CatalogIndexViewModel
        {
            Country = country,
            City = city,
            Search = name,
            Page = 1,
            TotalPages = 1,
            Universities = universities,
            Countries = (await _db.Countries
                .OrderBy(c => c.Name)
                .ToListAsync())
                .Select(c => _localization.GetLocalizedName(c.Name, "country"))
                .OrderBy(c => c)
                .ToList(),
            Cities = string.IsNullOrEmpty(country)
                ? []
                : (await _db.Cities
                    .Where(c => c.Country.Name == country || 
                        (_localization.IsBulgarianCulture() && _localization.GetLocalizedName(c.Country.Name, "country") == country))
                    .OrderBy(c => c.Name)
                    .ToListAsync())
                    .Select(c => _localization.GetLocalizedName(c.Name, "city"))
                    .OrderBy(c => c)
                    .ToList()
        };

        return View("~/Views/Catalog/Index.cshtml", vm);
    }

    [Authorize]
    public async Task<IActionResult> Favorites()
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var universities = await _db.Favourites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Include(f => f.University)
                .ThenInclude(u => u.Country)
            .Include(f => f.University)
                .ThenInclude(u => u.City)
            .Select(f => new CatalogUniversityViewModel
            {
                Id = f.University.Id,
                Name = _localization.GetLocalizedName(f.University.Name, "university"),
                ImageUrl = f.University.ImageUrl,
                Rating = f.University.Rating,
                AverageGpa = f.University.AverageGpa,
                Country = _localization.GetLocalizedName(f.University.Country.Name, "country"),
                City = _localization.GetLocalizedName(f.University.City.Name, "city")
            })
            .OrderByDescending(u => u.Rating)
            .ToListAsync();

        var vm = new CatalogIndexViewModel
        {
            Search = null,
            Country = null,
            City = null,
            Sort = null,
            Page = 1,
            TotalPages = 1,
            TotalCount = universities.Count,
            IsFavoritesPage = true, 
            Universities = universities,
            Countries = (await _db.Countries
                .OrderBy(c => c.Name)
                .ToListAsync())
                .Select(c => _localization.GetLocalizedName(c.Name, "country"))
                .OrderBy(c => c)
                .ToList(),
            Cities = []
        };

        ViewData["IsFavoritesPage"] = true;
        return View("~/Views/Universities/Index.cshtml", vm);
    }
}

