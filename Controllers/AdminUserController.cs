using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UniWebApp.ViewModels;
using Models.Entities;

namespace UniWebApp.Controllers.Admin;

[Authorize(Roles = "Admin")]
[IgnoreAntiforgeryToken]
public class AdminUserController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserListViewModel
            {
                Id = u.Id,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                ReviewsCount = u.Reviews.Count,
                FavouritesCount = u.Favourites.Count
            })
            .ToListAsync();

        return View("~/Views/Admin/Users/Index.cshtml", users);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _db.Users
            .Include(u => u.Reviews)
            .Include(u => u.Favourites)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        var model = new AdminUserListViewModel
        {
            Id = user.Id,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            ReviewsCount = user.Reviews.Count,
            FavouritesCount = user.Favourites.Count
        };

        return View("~/Views/Admin/Users/Details.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        var reviews = await _db.Reviews.Where(r => r.UserId == id).ToListAsync();
        var favourites = await _db.Favourites.Where(f => f.UserId == id).ToListAsync();

        _db.Reviews.RemoveRange(reviews);
        _db.Favourites.RemoveRange(favourites);
        _db.Users.Remove(user);

        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword([FromQuery] string id, [FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { success = false, message = "Password must be at least 6 characters long" });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (result.Succeeded)
            return Ok(new { success = true, message = "Password changed successfully" });
        
        return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
    }

    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
