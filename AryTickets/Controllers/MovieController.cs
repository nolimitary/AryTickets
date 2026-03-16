using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    public class MovieController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MovieController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["TMDb:ApiKey"];
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Details(int id)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var apiUrl = $"https://api.themoviedb.org/3/movie/{id}?api_key={_apiKey}&language=en-US&append_to_response=videos,credits,reviews";
            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var movie = JsonSerializer.Deserialize<Movie>(jsonResponse);

            var userId = _userManager.GetUserId(User);
            bool isFavorite = false;
            if (userId != null)
                isFavorite = await _context.UserFavorites.AnyAsync(f => f.UserId == userId && f.MovieId == id);

            ViewData["IsFavorite"] = isFavorite;

            // Load user reviews from our DB
            var userReviews = await _context.UserReviews
                .Where(r => r.MovieId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewData["UserReviews"] = userReviews;

            // Check if current user already reviewed
            if (userId != null)
                ViewData["HasReviewed"] = await _context.UserReviews.AnyAsync(r => r.UserId == userId && r.MovieId == id);
            else
                ViewData["HasReviewed"] = false;

            // Load showtimes from DB
            var showtimes = await _context.Showtimes
                .Where(s => s.TmdbMovieId == id && s.IsActive && s.ShowDateTime > System.DateTime.UtcNow)
                .OrderBy(s => s.ShowDateTime)
                .ToListAsync();
            ViewData["Showtimes"] = showtimes;

            return View(movie);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int movieId, string movieTitle, int rating, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var existing = await _context.UserReviews.AnyAsync(r => r.UserId == user.Id && r.MovieId == movieId);
            if (existing) return RedirectToAction("Details", new { id = movieId });

            var review = new UserReview
            {
                UserId = user.Id,
                UserName = user.UserName,
                IsCritic = user.IsCritic,
                MovieId = movieId,
                MovieTitle = movieTitle,
                Rating = rating,
                Content = content,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.UserReviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = movieId });
        }

        // Movie Search
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View(new List<Movie>());

            var httpClient = _httpClientFactory.CreateClient();
            var searchUrl = $"https://api.themoviedb.org/3/search/movie?api_key={_apiKey}&language=en-US&query={System.Uri.EscapeDataString(query)}&page=1";
            var response = await httpClient.GetAsync(searchUrl);

            var movies = new List<Movie>();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResult>(json);
                movies = result?.Results ?? new List<Movie>();
            }

            ViewData["SearchQuery"] = query;
            return View(movies);
        }
    }
}
