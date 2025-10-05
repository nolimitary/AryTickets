using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        private async Task<ProfileViewModel> GetUserProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            ViewData["Username"] = user.UserName;
            ViewData["Email"] = user.Email;

            return new ProfileViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                JoinDate = user.LockoutEnd?.DateTime ?? DateTime.UtcNow,
            };
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = await GetUserProfile();
            return View(viewModel);
        }

        public async Task<IActionResult> Reviews()
        {
            var viewModel = await GetUserProfile();
            return View(viewModel);
        }

        public async Task<IActionResult> Favorites()
        {
            var viewModel = await GetUserProfile();
            return View(viewModel);
        }

        public async Task<IActionResult> Settings()
        {
            var viewModel = await GetUserProfile();
            return View(viewModel);
        }
    }
}