using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        private async Task SetUserViewData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewData["Username"] = user.UserName;
                ViewData["Email"] = user.Email;
                ViewData["IsCritic"] = user.IsCritic;
                ViewData["MemberSince"] = user.CreatedAt.Year > 2000
                    ? user.CreatedAt.ToString("MMMM yyyy")
                    : "Early member";
            }
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
            await SetUserViewData();
            var userId = user.Id;

            var ticketCount = await _context.Bookings.CountAsync(b => b.UserId == userId);
            var favoritesCount = await _context.UserFavorites.CountAsync(f => f.UserId == userId);
            var totalSpent = (decimal)(await _context.Bookings
                .Where(b => b.UserId == userId)
                .Select(b => (double)b.TotalPrice)
                .ToListAsync()).Sum();
            var recentBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .Take(5)
                .ToListAsync();

            var reviewsCount = await _context.UserReviews.CountAsync(r => r.UserId == userId);

            var existingApp = await _context.CriticApplications
                .FirstOrDefaultAsync(a => a.UserId == userId);

            var model = new ProfileViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                JoinDate = user.CreatedAt,
                TicketCount = ticketCount,
                FavoritesCount = favoritesCount,
                ReviewsCount = reviewsCount,
                TotalSpent = totalSpent,
                RecentBookings = recentBookings,
                IsCritic = user.IsCritic,
                HasPendingApplication = existingApp?.Status == ApplicationStatus.Pending,
                HasApplication = existingApp != null
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CriticApplication()
        {
            await SetUserViewData();
            var user = await _userManager.GetUserAsync(User);

            if (user.IsCritic)
                return RedirectToAction("Index");

            var existing = await _context.CriticApplications
                .FirstOrDefaultAsync(a => a.UserId == user.Id);

            if (existing != null)
            {
                ViewData["ApplicationStatus"] = existing.Status;
                return View("CriticApplicationStatus", existing);
            }

            return View(new CriticApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriticApplication(CriticApplicationViewModel model)
        {
            await SetUserViewData();
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);

            model.ReviewLinks = model.ReviewLinks?.Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
                ?? new System.Collections.Generic.List<string>();

            var application = new Models.CriticApplication
            {
                UserId = userId,
                FullName = model.FullName,
                Bio = model.Bio,
                YearsOfExperience = model.YearsOfExperience,
                PastEmployers = model.PastEmployers,
                ReviewLinksJson = JsonSerializer.Serialize(model.ReviewLinks),
                Motivation = model.Motivation,
                Status = ApplicationStatus.Pending,
                AppliedAt = System.DateTime.UtcNow
            };

            _context.CriticApplications.Add(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your critic application has been submitted for review!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> BookingHistory()
        {
            await SetUserViewData();
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();
            return View(bookings);
        }

        public async Task<IActionResult> Reviews()
        {
            await SetUserViewData();
            return View();
        }

        public async Task<IActionResult> Favorites()
        {
            await SetUserViewData();
            var userId = _userManager.GetUserId(User);
            var favoriteMovies = await _context.UserFavorites
                                            .Where(f => f.UserId == userId)
                                            .ToListAsync();
            return View(favoriteMovies);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new SettingsViewModel
            {
                Username = user.UserName,
                Email = user.Email
            };
            await SetUserViewData();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (user.UserName != model.Username)
            {
                var setUsernameResult = await _userManager.SetUserNameAsync(user, model.Username);
                if (!setUsernameResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", setUsernameResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Settings");
                }
            }

            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", setEmailResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Settings");
                }
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Settings");
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Error deleting account.";
            return RedirectToAction("Settings");
        }
    }
}