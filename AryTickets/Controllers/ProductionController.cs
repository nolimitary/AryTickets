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
    public class ProductionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Details(int id)
        {
            var production = await _context.Productions.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (production == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            bool isFavorite = false;
            if (userId != null)
                isFavorite = await _context.UserFavorites.AnyAsync(f => f.UserId == userId && f.ProductionId == id);
            ViewData["IsFavorite"] = isFavorite;

            var userReviews = await _context.UserReviews
                .Where(r => r.ProductionId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewData["UserReviews"] = userReviews;

            if (userId != null)
                ViewData["HasReviewed"] = await _context.UserReviews.AnyAsync(r => r.UserId == userId && r.ProductionId == id);
            else
                ViewData["HasReviewed"] = false;

            var performances = await _context.Performances
                .Where(p => p.ProductionId == id && p.IsActive && p.ShowDateTime > System.DateTime.UtcNow)
                .OrderBy(p => p.ShowDateTime)
                .ToListAsync();
            ViewData["Performances"] = performances;

            return View(production);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int productionId, string productionTitle, int rating, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var existing = await _context.UserReviews.AnyAsync(r => r.UserId == user.Id && r.ProductionId == productionId);
            if (existing) return RedirectToAction("Details", new { id = productionId });

            var review = new UserReview
            {
                UserId = user.Id,
                UserName = user.UserName,
                IsCritic = user.IsCritic,
                ProductionId = productionId,
                ProductionTitle = productionTitle,
                Rating = rating,
                Content = content,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.UserReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = productionId });
        }

        public async Task<IActionResult> Search(string query)
        {
            ViewData["SearchQuery"] = query;

            if (string.IsNullOrWhiteSpace(query))
                return View(new System.Collections.Generic.List<Production>());

            var q = query.Trim();
            var results = await _context.Productions
                .Where(p => p.IsActive && (
                    p.Title.Contains(q) ||
                    p.TitleOriginal.Contains(q) ||
                    p.Playwright.Contains(q) ||
                    p.Director.Contains(q) ||
                    p.Genre.Contains(q)))
                .OrderBy(p => p.Title)
                .ToListAsync();

            return View(results);
        }
    }
}
