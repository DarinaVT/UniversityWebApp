using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniWebApp.ViewModels;

namespace UniWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly AppDbContext _db;

    public AdminDashboardController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var adminRoleId = await _db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();
        
        var adminUserIds = adminRoleId != null
            ? await _db.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync()
            : new List<string>();

        var vm = new AdminDashboardViewModel
        {
            UniversitiesCount = await _db.Universities.CountAsync(),
            UsersCount = await _db.Users.CountAsync(u => !adminUserIds.Contains(u.Id)),
            PendingReviews = await _db.Reviews.CountAsync(r => !r.IsApproved),
            CountriesCount = await _db.Countries.CountAsync(),
            ViewsToday = await _db.Reviews.CountAsync(r => r.CreatedAt >= today),
            ViewsThisWeek = await _db.Reviews.CountAsync(r => r.CreatedAt >= weekStart),
            ViewsThisMonth = await _db.Reviews.CountAsync(r => r.CreatedAt >= monthStart),
            TotalViews = await _db.Reviews.CountAsync()
        };

        return View("~/Views/Admin/Index.cshtml", vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUniversitiesForMap()
    {
        var universities = await _db.Universities
            .AsNoTracking()
            .Where(u => u.Latitude != null && u.Longitude != null)
            .OrderByDescending(u => u.Rating)
            .Take(100)
            .Select(u => new
            {
                id = u.Id,
                name = u.Name,
                latitude = u.Latitude,
                longitude = u.Longitude,
                rating = u.Rating
            })
            .ToListAsync();

        return Json(universities);
    }
}
