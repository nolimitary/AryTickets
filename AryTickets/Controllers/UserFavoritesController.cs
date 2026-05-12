using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    [Authorize]
    public class UserFavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserFavoritesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int productionId, string productionTitle, string posterUrl)
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductionId == productionId);

            if (existing != null)
            {
                _context.UserFavorites.Remove(existing);
            }
            else
            {
                _context.UserFavorites.Add(new UserFavorite
                {
                    UserId = userId,
                    ProductionId = productionId,
                    ProductionTitle = productionTitle,
                    PosterUrl = posterUrl
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Production", new { id = productionId });
        }
    }
}
