using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Services;
using UniWebApp.ViewModels.Catalog;

namespace UniWebApp.Controllers;

public class CatalogController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILocalizationService _localization;
    private const int PageSize = 12;

    public CatalogController(AppDbContext db, ILocalizationService localization)
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
            "rating-asc" => query.OrderBy(u => u.Rating),
            "rating-desc" => query.OrderByDescending(u => u.Rating),
            "gpa-asc" => query.OrderBy(u => u.AverageGpa),
            "gpa-desc" => query.OrderByDescending(u => u.AverageGpa),
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
                Country = _localization.GetLocalizedName(u.Country.Name, "country"),
                City = _localization.GetLocalizedName(u.City.Name, "city")
            })
            .ToListAsync();

        var vm = new CatalogIndexViewModel
        {
            Country = country,
            City = city,
            Search = search,
            Sort = sort ?? "rating-desc",
            Page = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize),
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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_CatalogResults", vm);

        return View(vm);
    }
    [HttpGet]
    public async Task<IActionResult> TopCountries()
    {
        var countries = (await _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .GroupBy(u => u.Country.Name)
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select(g => g.Key)
            .ToListAsync())
            .Select(c => _localization.GetLocalizedName(c, "country"))
            .ToList();

        return Json(countries);
    }

    [HttpGet]
    public async Task<IActionResult> SearchCountries(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new List<string>());

        var qLower = q.ToLower();
        var countries = (await _db.Countries
            .AsNoTracking()
            .ToListAsync())
            .Where(c => 
                c.Name.ToLower().StartsWith(qLower) || 
                _localization.MatchesSearch(c.Name, q))
            .OrderBy(c => _localization.GetLocalizedName(c.Name, "country"))
            .Take(10)
            .Select(c => _localization.GetLocalizedName(c.Name, "country"))
            .ToList();

        return Json(countries);
    }

    [HttpGet]
    public async Task<IActionResult> TopCitiesByCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return Json(new List<string>());

        var allCountries = await _db.Countries.ToListAsync();
        var matchingCountryNames = allCountries
            .Where(c => 
                c.Name == country || 
                _localization.MatchesSearch(c.Name, country) ||
                _localization.GetLocalizedName(c.Name, "country") == country)
            .Select(c => c.Name)
            .ToList();

        if (!matchingCountryNames.Any())
            return Json(new List<string>());

        var cities = (await _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .Include(u => u.City)
            .Where(u => matchingCountryNames.Contains(u.Country.Name))
            .GroupBy(u => u.City.Name)
            .OrderByDescending(g => g.Count())
            .Take(6)
            .Select(g => g.Key)
            .ToListAsync())
            .Select(c => _localization.GetLocalizedName(c, "city"))
            .ToList();

        return Json(cities);
    }

    [HttpGet]
    public async Task<IActionResult> SearchCities(string country, string q)
    {
        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(q))
            return Json(new List<string>());

        var allCountries = await _db.Countries.ToListAsync();
        var matchingCountryNames = allCountries
            .Where(c => 
                c.Name == country || 
                _localization.MatchesSearch(c.Name, country) ||
                _localization.GetLocalizedName(c.Name, "country") == country)
            .Select(c => c.Name)
            .ToList();

        if (!matchingCountryNames.Any())
            return Json(new List<string>());

        var cities = (await _db.Cities
            .AsNoTracking()
            .Include(c => c.Country)
            .Where(c => matchingCountryNames.Contains(c.Country.Name))
            .ToListAsync())
            .Where(c => 
                c.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase) || 
                _localization.MatchesSearch(c.Name, q))
            .OrderBy(c => _localization.GetLocalizedName(c.Name, "city"))
            .Take(10)
            .Select(c => _localization.GetLocalizedName(c.Name, "city"))
            .ToList();

        return Json(cities);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCitiesByCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return Json(new List<string>());

        var cities = (await _db.Cities
            .AsNoTracking()
            .Include(c => c.Country)
            .Where(c => c.Country.Name == country || 
                (_localization.IsBulgarianCulture() && _localization.GetLocalizedName(c.Country.Name, "country") == country))
            .OrderBy(c => c.Name)
            .ToListAsync())
            .Select(c => _localization.GetLocalizedName(c.Name, "city"))
            .OrderBy(c => c)
            .ToList();

        return Json(cities);
    }

}