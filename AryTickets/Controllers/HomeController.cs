using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index(string genre = null)
        {
            var now = System.DateTime.UtcNow;

            // A production is "in repertoire" if it has any upcoming active performance
            var productionsWithUpcoming = await _db.Performances
                .Where(p => p.IsActive && p.ShowDateTime > now)
                .Select(p => p.ProductionId)
                .Distinct()
                .ToListAsync();

            var productionsQuery = _db.Productions.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(genre))
                productionsQuery = productionsQuery.Where(p => p.Genre == genre);

            var allProductions = await productionsQuery.OrderByDescending(p => p.Rating).ToListAsync();

            var currentRepertoire = allProductions
                .Where(p => productionsWithUpcoming.Contains(p.Id))
                .ToList();

            var upcomingPremieres = allProductions
                .Where(p => !productionsWithUpcoming.Contains(p.Id) && p.PremiereDate > now)
                .OrderBy(p => p.PremiereDate)
                .ToList();

            var allGenres = await _db.Productions
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Genre))
                .Select(p => p.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            var viewModel = new HomeViewModel
            {
                CurrentRepertoire = currentRepertoire,
                UpcomingPremieres = upcomingPremieres,
                AllGenres = allGenres,
                SelectedGenre = genre
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Home/StatusCode/{code:int}")]
        public IActionResult StatusCode(int code)
        {
            return code switch
            {
                403 => View("AccessDenied"),
                404 => View("NotFound"),
                _ => View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier })
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
