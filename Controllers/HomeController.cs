using Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services;
using UniWebApp.ViewModels.Home;

namespace UniWebApp.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILocalizationService _localization;

    public HomeController(AppDbContext db, ILocalizationService localization)
    {
        _db = db;
        _localization = localization;
    }

    public async Task<IActionResult> Index()
    {
        var universities = await _db.Universities
            .AsNoTracking()
            .Include(u => u.City)
            .Include(u => u.Country)
            .OrderByDescending(u => u.Rating)
            .Take(8)
            .ToListAsync();

        var universitiesVm = universities.Select(u => new HomeUniversityViewModel
        {
            Id = u.Id,
            Slug = (u.Slug == null || string.IsNullOrEmpty(u.Slug)) ? u.Id.ToString() : u.Slug,
            Name = _localization.GetLocalizedName(u.Name, "university"),
            Country = _localization.GetLocalizedName(u.Country.Name, "country"),
            City = _localization.GetLocalizedName(u.City.Name, "city"),
            ImageUrl = u.ImageUrl,
            Rating = u.Rating,
            AverageGpa = u.AverageGpa
        }).ToList();

        var countries = await _db.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        var countriesList = countries
            .Select(c => _localization.GetLocalizedName(c.Name, "country"))
            .OrderBy(c => c)
            .ToList();

        var vm = new HomeIndexViewModel
        {
            FeaturedUniversities = universitiesVm,
            Countries = countriesList
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> TopCountriesStats()
    {
        var data = await _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .GroupBy(u => u.Country)
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var result = data.Select(x => new HomeCountryStatsViewModel
        {
            Country = _localization.GetLocalizedName(x.Country.Name, "country"),
            UniversitiesCount = x.Count
        }).ToList();

        return Json(result);
    }


    [HttpGet]
    public async Task<IActionResult> TopUniversitiesByCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return BadRequest();

        country = country.Trim();

        var normalized = country switch
        {
            "United States of America" => "United States",
            _ => country
        };

        var allCountries = await _db.Countries
            .AsNoTracking()
            .ToListAsync();
        
        var exactMatch = allCountries
            .FirstOrDefault(c => 
                c.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                _localization.GetLocalizedName(c.Name, "country").Equals(normalized, StringComparison.OrdinalIgnoreCase));
        
        List<string> matchingCountryNames;
        
        if (exactMatch != null)
        {
            matchingCountryNames = new List<string> { exactMatch.Name };
        }
        else
        {
            matchingCountryNames = allCountries
                .Where(c => 
                {
                    var englishName = c.Name;
                    var localizedName = _localization.GetLocalizedName(c.Name, "country");
                    
                    if (englishName.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                        localizedName.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                        return true;
                    
                    var normalizedLower = normalized.ToLower();
                    var englishLower = englishName.ToLower();
                    var localizedLower = localizedName.ToLower();
                    
                    if (englishLower == normalizedLower || localizedLower == normalizedLower)
                        return true;
                    
                    if (englishLower.StartsWith(normalizedLower + " ") || 
                        localizedLower.StartsWith(normalizedLower + " "))
                        return true;
                    
                    return _localization.MatchesSearch(c.Name, normalized);
                })
                .Select(c => c.Name)
                .Distinct()
                .ToList();
        }

        if (!matchingCountryNames.Any())
        {
            return Json(new List<MapUniversityPinViewModel>());
        }

        var universities = await _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .Where(u => matchingCountryNames.Contains(u.Country.Name))
            .OrderByDescending(u => u.Rating)
            .Take(6)
            .ToListAsync();

        var result = universities
            .Where(u => 
            {
                var lat = u.Latitude;
                var lng = u.Longitude;
                
                if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
                    return false;
                
                if (Math.Abs(lat) > 90)
                    return false;
                
                return true;
            })
            .Select(u => new MapUniversityPinViewModel
            {
                Id = u.Id,
                Name = _localization.GetLocalizedName(u.Name, "university"),
                Country = _localization.GetLocalizedName(u.Country.Name, "country"),
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                Rating = u.Rating
            })
            .ToList();

        return Json(result);
    }


    [HttpPost]
    public IActionResult SetLanguage(string culture)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            }
        );

        return Redirect(Request.Headers["Referer"].ToString());
    }

    [HttpGet]
    public async Task<IActionResult> SearchSuggestions(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new { universities = new List<object>(), countries = new List<string>(), cities = new List<string>() });

        q = q.Trim();

        var qLower = q.ToLower();
        
        var allUniversities = await _db.Universities
            .AsNoTracking()
            .Include(u => u.Country)
            .Include(u => u.City)
            .ToListAsync();

        var universities = allUniversities
            .Where(u => 
                u.Name.ToLower().Contains(qLower) ||
                _localization.MatchesSearch(u.Name, q))
            .OrderByDescending(u => u.Rating)
            .Take(5)
            .ToList();

        var universitiesResult = universities.Select(u => new
        {
            id = u.Id,
            name = _localization.GetLocalizedName(u.Name, "university"),
            country = _localization.GetLocalizedName(u.Country.Name, "country"),
            city = _localization.GetLocalizedName(u.City.Name, "city"),
            type = "university"
        }).ToList();

        var allCountries = await _db.Countries
            .AsNoTracking()
            .ToListAsync();

        var countries = allCountries
            .Where(c => 
                c.Name.ToLower().Contains(qLower) ||
                _localization.MatchesSearch(c.Name, q))
            .OrderBy(c => c.Name)
            .Take(5)
            .ToList();

        var countriesResult = countries
            .Select(c => _localization.GetLocalizedName(c.Name, "country"))
            .ToList();

        var allCities = await _db.Cities
            .AsNoTracking()
            .Include(c => c.Country)
            .ToListAsync();

        var cities = allCities
            .Where(c => 
                c.Name.ToLower().Contains(qLower) ||
                _localization.MatchesSearch(c.Name, q))
            .OrderBy(c => c.Name)
            .Take(5)
            .ToList();

        var citiesResult = cities.Select(c => new
        {
            name = _localization.GetLocalizedName(c.Name, "city"),
            country = _localization.GetLocalizedName(c.Country.Name, "country")
        }).ToList();

        return Json(new
        {
            universities = universitiesResult,
            countries = countriesResult,
            cities = citiesResult
        });
    }
}