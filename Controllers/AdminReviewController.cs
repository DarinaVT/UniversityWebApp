using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UniWebApp.Controllers;

[Authorize(Roles = "Admin")]
[IgnoreAntiforgeryToken]
public class AdminReviewController : Controller
{
    private readonly AppDbContext _db;

    public AdminReviewController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var pendingReviews = await _db.Reviews
            .Include(r => r.University)
            .Include(r => r.User)
            .Where(r => !r.IsApproved)
            .ToListAsync();

        return View("~/Views/Admin/AdminReview/Index.cshtml", pendingReviews);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var review = await _db.Reviews
            .Include(r => r.University)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (review == null)
            return NotFound();

        review.IsApproved = true;
        await _db.SaveChangesAsync();

        await RecalculateUniversityRating(review.UniversityId);

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        var review = await _db.Reviews
            .Include(r => r.University)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (review == null)
            return NotFound();

        var universityId = review.UniversityId;
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();

        await RecalculateUniversityRating(universityId);

        return Ok();
    }

    private async Task RecalculateUniversityRating(int universityId)
    {
        var approvedReviews = await _db.Reviews
            .Where(r => r.UniversityId == universityId && r.IsApproved)
            .ToListAsync();

        if (approvedReviews.Any())
        {
            var averageRating = approvedReviews.Average(r => (decimal)r.Rating);
            var university = await _db.Universities.FindAsync(universityId);
            if (university != null)
            {
                university.Rating = Math.Round(averageRating, 2);
                await _db.SaveChangesAsync();
            }
        }
    }
}