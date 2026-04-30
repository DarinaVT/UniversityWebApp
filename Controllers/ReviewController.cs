using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using System.Security.Claims;
using UniWebApp.Extensions;
using UniWebApp.ViewModels;

namespace UniWebApp.Controllers
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ReviewController : Controller
    {
        private readonly AppDbContext _db;

        public ReviewController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Add(int universityId, int rating, string comment)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized();
                }

                if (universityId <= 0)
                {
                    return BadRequest("Invalid university ID");
                }

                if (rating < 1 || rating > 5)
                {
                    return BadRequest("Rating must be between 1 and 5");
                }

                if (string.IsNullOrWhiteSpace(comment))
                {
                    return BadRequest("Comment is required");
                }

                var universityExists = await _db.Universities.AnyAsync(u => u.Id == universityId);
                if (!universityExists)
                {
                    return NotFound("University not found");
                }

                var review = new Review
                {
                    UniversityId = universityId,
                    Rating = rating,
                    Comment = comment.Trim(),
                    UserId = userId,
                    IsApproved = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Reviews.Add(review);
                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = "Review submitted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error saving review: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> List(int universityId)
        {
            var reviews = await _db.Reviews
                .Include(r => r.User)
                .Where(r => r.UniversityId == universityId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return PartialView("_ReviewsList", reviews);
        }

    }
}