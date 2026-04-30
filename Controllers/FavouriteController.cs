using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using UniWebApp.Extensions;

namespace UniWebApp.Controllers;

[Authorize]
[IgnoreAntiforgeryToken]
public class FavouriteController : Controller
{
    private readonly AppDbContext _db;

    public FavouriteController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int universityId)
    {
        var userId = User.GetUserId();
        
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (universityId <= 0)
            return BadRequest("Invalid university ID");

        var universityExists = await _db.Universities.AnyAsync(u => u.Id == universityId);
        if (!universityExists)
            return NotFound("University not found");

        var favourite = await _db.Favourites
            .FirstOrDefaultAsync(f =>
                f.UniversityId == universityId &&
                f.UserId == userId);

        if (favourite == null)
        {
            _db.Favourites.Add(new Favourite
            {
                UniversityId = universityId,
                UserId = userId
            });

            await _db.SaveChangesAsync();
            return Json(new { isFavourite = true, success = true });
        }

        _db.Favourites.Remove(favourite);
        await _db.SaveChangesAsync();

        return Json(new { isFavourite = false, success = true });
    }
}