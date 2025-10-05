using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
            }
        }

        public async Task<IActionResult> Index()
        {
            await SetUserViewData();
            return View();
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
                    var errorMessage = string.Join(" ", setUsernameResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction("Settings");
                }
            }

            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    var errorMessage = string.Join(" ", setEmailResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = errorMessage;
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

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var errorMessage = string.Join(" ", changePasswordResult.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = errorMessage;
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