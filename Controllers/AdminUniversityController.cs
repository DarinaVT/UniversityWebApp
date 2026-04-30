using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniWebApp.ViewModels.Admin;
using Services;
using Models.Entities;

namespace UniWebApp.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class AdminUniversityController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILocalizationService _localization;
    private readonly ITranslationService _translationService;

    public AdminUniversityController(AppDbContext db, ILocalizationService localization, ITranslationService translationService)
    {
        _db = db;
        _localization = localization;
        _translationService = translationService;
    }

    private const int PageSize = 20;

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        var query = _db.Universities
            .Include(u => u.Country)
            .Include(u => u.City)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u => 
                u.Name.ToLower().Contains(searchLower) ||
                u.Country.Name.ToLower().Contains(searchLower) ||
                u.City.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var universities = (await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync())
            .Select(u => new AdminUniversityListViewModel
            {
                Id = u.Id,
                Name = _localization.GetLocalizedName(u.Name, "university"),
                Country = _localization.GetLocalizedName(u.Country.Name, "country"),
                City = _localization.GetLocalizedName(u.City.Name, "city"),
                Rating = u.Rating
            })
            .ToList();

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        return View("~/Views/Admin/Universities/Index.cshtml", universities);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var uni = await _db.Universities
            .Include(u => u.Country)
            .Include(u => u.City)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (uni == null) return NotFound();

        var countries = await _db.Countries.OrderBy(c => c.Name).ToListAsync();
        var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();

        var countriesWithTranslation = countries.Select(c => new CountryCityOptionViewModel
        {
            Id = c.Id,
            Name = c.Name,
            TranslatedName = _localization.GetLocalizedName(c.Name, "country")
        }).OrderBy(c => c.TranslatedName).ToList();

        var citiesWithTranslation = cities.Select(c => new CountryCityOptionViewModel
        {
            Id = c.Id,
            Name = c.Name,
            TranslatedName = _localization.GetLocalizedName(c.Name, "city"),
            CountryId = c.CountryId
        }).OrderBy(c => c.TranslatedName).ToList();

        ViewBag.Countries = countriesWithTranslation;
        ViewBag.Cities = citiesWithTranslation;
        ViewBag.University = uni;

        return View("~/Views/Admin/Universities/Edit.cshtml", new AdminUniversityEditViewModel
        {
            Id = uni.Id,
            Name = uni.Name,
            CountryId = uni.CountryId,
            CityId = uni.CityId,
            Rating = uni.Rating,
            AverageGpa = uni.AverageGpa,
            ImageUrl = uni.ImageUrl ?? ""
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(AdminUniversityEditViewModel model, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            var countries = await _db.Countries.OrderBy(c => c.Name).ToListAsync();
            var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();
            var existingUni = await _db.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .FirstOrDefaultAsync(u => u.Id == model.Id);

            var countriesWithTranslation = countries.Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "country")
            }).OrderBy(c => c.TranslatedName).ToList();

            var citiesWithTranslation = cities.Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "city"),
                CountryId = c.CountryId
            }).OrderBy(c => c.TranslatedName).ToList();

            ViewBag.Countries = countriesWithTranslation;
            ViewBag.Cities = citiesWithTranslation;
            ViewBag.University = existingUni;
            return View("~/Views/Admin/Universities/Edit.cshtml", model);
        }

        var uni = await _db.Universities.FindAsync(model.Id);
        if (uni == null) return NotFound();

        var existingUniversity = await _db.Universities
            .FirstOrDefaultAsync(u => 
                u.Id != model.Id &&
                u.Name.ToLower() == model.Name.ToLower() &&
                u.CountryId == model.CountryId &&
                u.CityId == model.CityId);

        if (existingUniversity != null)
        {
            ModelState.AddModelError("", "A university with this name already exists in the selected country and city.");
            var countries = await _db.Countries.OrderBy(c => c.Name).ToListAsync();
            var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();

            var countriesWithTranslation = countries.Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "country")
            }).OrderBy(c => c.TranslatedName).ToList();

            var citiesWithTranslation = cities.Select(c => new
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "city"),
                CountryId = c.CountryId
            }).OrderBy(c => c.TranslatedName).ToList();

            ViewBag.Countries = countriesWithTranslation;
            ViewBag.Cities = citiesWithTranslation;
            ViewBag.University = uni;
            return View("~/Views/Admin/Universities/Edit.cshtml", model);
        }

        var oldName = uni.Name;
        
        var (englishName, bulgarianName) = await _translationService.TranslateUniversityNameAsync(model.Name);
        
        uni.Name = englishName;
        uni.CountryId = model.CountryId;
        uni.CityId = model.CityId;
        uni.Rating = model.Rating;
        uni.AverageGpa = model.AverageGpa;
        uni.GPARequirement = (decimal)model.AverageGpa;

        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "universities");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{uni.Id}_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            uni.ImageUrl = $"/images/universities/{fileName}";
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            uni.ImageUrl = model.ImageUrl;
        }

        await _db.SaveChangesAsync();

        if (oldName != englishName)
        {
            await _localization.AddOrUpdateTranslationAsync(englishName, bulgarianName, "university");
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Create()
    {
        var countries = await _db.Countries.OrderBy(c => c.Name).ToListAsync();
        var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();

        var countriesWithTranslation = countries.Select(c => new CountryCityOptionViewModel
        {
            Id = c.Id,
            Name = c.Name,
            TranslatedName = _localization.GetLocalizedName(c.Name, "country")
        }).OrderBy(c => c.TranslatedName).ToList();

        var citiesWithTranslation = cities.Select(c => new CountryCityOptionViewModel
        {
            Id = c.Id,
            Name = c.Name,
            TranslatedName = _localization.GetLocalizedName(c.Name, "city"),
            CountryId = c.CountryId
        }).OrderBy(c => c.TranslatedName).ToList();

        ViewBag.Countries = countriesWithTranslation;
        ViewBag.Cities = citiesWithTranslation;

        return View("~/Views/Admin/Universities/Create.cshtml", new AdminUniversityEditViewModel
        {
            Id = 0,
            Rating = 0,
            AverageGpa = 0
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdminUniversityEditViewModel model, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            var countries = await _db.Countries.OrderBy(c => c.Name).ToListAsync();
            var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();

            var countriesWithTranslation = countries.Select(c => new CountryCityOptionViewModel
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "country")
            }).OrderBy(c => c.TranslatedName).ToList();

            var citiesWithTranslation = cities.Select(c => new CountryCityOptionViewModel
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "city"),
                CountryId = c.CountryId
            }).OrderBy(c => c.TranslatedName).ToList();

            ViewBag.Countries = countriesWithTranslation;
            ViewBag.Cities = citiesWithTranslation;
            return View("~/Views/Admin/Universities/Create.cshtml", model);
        }

        var existingUniversity = await _db.Universities
            .FirstOrDefaultAsync(u => 
                u.Name.ToLower() == model.Name.ToLower() &&
                u.CountryId == model.CountryId &&
                u.CityId == model.CityId);

        if (existingUniversity != null)
        {
            ModelState.AddModelError("", "A university with this name already exists in the selected country and city.");
            var countries = await _db.Countries.OrderBy(c => c.Name).ToListAsync();
            var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();

            var countriesWithTranslation = countries.Select(c => new CountryCityOptionViewModel
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "country")
            }).OrderBy(c => c.TranslatedName).ToList();

            var citiesWithTranslation = cities.Select(c => new CountryCityOptionViewModel
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "city"),
                CountryId = c.CountryId
            }).OrderBy(c => c.TranslatedName).ToList();

            ViewBag.Countries = countriesWithTranslation;
            ViewBag.Cities = citiesWithTranslation;
            return View("~/Views/Admin/Universities/Create.cshtml", model);
        }

        var country = await _db.Countries.FindAsync(model.CountryId);
        var city = await _db.Cities.FindAsync(model.CityId);

        if (country == null || city == null)
        {
            ModelState.AddModelError("", "Invalid country or city selected.");
            var countries = await _db.Countries.OrderBy(c => c.Name).ToListAsync();
            var cities = await _db.Cities.OrderBy(c => c.Name).ToListAsync();

            var countriesWithTranslation = countries.Select(c => new CountryCityOptionViewModel
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "country")
            }).OrderBy(c => c.TranslatedName).ToList();

            var citiesWithTranslation = cities.Select(c => new CountryCityOptionViewModel
            {
                Id = c.Id,
                Name = c.Name,
                TranslatedName = _localization.GetLocalizedName(c.Name, "city"),
                CountryId = c.CountryId
            }).OrderBy(c => c.TranslatedName).ToList();

            ViewBag.Countries = countriesWithTranslation;
            ViewBag.Cities = citiesWithTranslation;
            return View("~/Views/Admin/Universities/Create.cshtml", model);
        }

        var (englishName, bulgarianName) = await _translationService.TranslateUniversityNameAsync(model.Name);

        var university = new University
        {
            Name = englishName,
            CountryId = model.CountryId,
            CityId = model.CityId,
            Rating = model.Rating,
            AverageGpa = model.AverageGpa,
            GPARequirement = (decimal)model.AverageGpa,
            Latitude = 0,
            Longitude = 0
        };

        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "universities");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            university.ImageUrl = $"/images/universities/{fileName}";
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            university.ImageUrl = model.ImageUrl;
        }
        else
        {
            university.ImageUrl = null;
        }

        _db.Universities.Add(university);
        await _db.SaveChangesAsync();

        await _localization.AddOrUpdateTranslationAsync(englishName, bulgarianName, "university");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var uni = await _db.Universities.FindAsync(id);
        if (uni == null) return NotFound();

        _db.Universities.Remove(uni);
        await _db.SaveChangesAsync();

        return Ok();
    }
}
