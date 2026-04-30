#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using System.IO;
using Services;

namespace UniWebApp.Areas.Identity.Pages.Account.Manage
{
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Infrastructure.AppDbContext _db;
        private readonly ILocalizationService _localization;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            Infrastructure.AppDbContext db,
            ILocalizationService localization)
        {
            _userManager = userManager;
            _db = db;
            _localization = localization;
        }

        public List<UserReviewViewModel> ApprovedReviews { get; set; } = new();
        public List<UserReviewViewModel> PendingReviews { get; set; } = new();
        public int TotalReviews { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int FavouritesCount { get; set; }

        public class UserReviewViewModel
        {
            public int Id { get; set; }
            public string UniversityName { get; set; }
            public int UniversityId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsApproved { get; set; }
        }

        public bool IsAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (IsAdmin)
            {
                TotalReviews = 0;
                ApprovedCount = 0;
                PendingCount = 0;
                ApprovedReviews = new List<UserReviewViewModel>();
                PendingReviews = new List<UserReviewViewModel>();
                return Page();
            }

            var userId = user.Id;

            var allReviews = await _db.Reviews
                .Include(r => r.University)
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedOn)
                .ToListAsync();

            TotalReviews = allReviews.Count;
            ApprovedCount = allReviews.Count(r => r.IsApproved);
            PendingCount = allReviews.Count(r => !r.IsApproved);
            
            FavouritesCount = await _db.Favourites
                .CountAsync(f => f.UserId == userId);

            ApprovedReviews = allReviews
                .Where(r => r.IsApproved)
                .Select(r => new UserReviewViewModel
                {
                    Id = r.Id,
                    UniversityName = _localization.GetLocalizedName(r.University.Name, "university"),
                    UniversityId = r.University.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedOn,
                    IsApproved = true
                })
                .ToList();

            PendingReviews = allReviews
                .Where(r => !r.IsApproved)
                .Select(r => new UserReviewViewModel
                {
                    Id = r.Id,
                    UniversityName = _localization.GetLocalizedName(r.University.Name, "university"),
                    UniversityId = r.University.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedOn,
                    IsApproved = false
                })
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostChangePhotoAsync(IFormFile photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (photo != null && photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                user.ProfilePictureUrl = $"/images/profiles/{fileName}";
                await _userManager.UpdateAsync(user);
            }

            return RedirectToPage();
        }
    }
}

