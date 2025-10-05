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
        public async Task<IActionResult> ToggleFavorite(int movieId, string movieTitle, string posterPath)
        {
            var userId = _userManager.GetUserId(User);
            var existingFavorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId);

            if (existingFavorite != null)
            {
                _context.UserFavorites.Remove(existingFavorite);
            }
            else
            {
                var newFavorite = new UserFavorite
                {
                    UserId = userId,
                    MovieId = movieId,
                    MovieTitle = movieTitle,
                    PosterPath = posterPath
                };
                _context.UserFavorites.Add(newFavorite);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Movie", new { id = movieId });
        }
    }
}