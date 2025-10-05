using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            {
                return NotFound();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var movie = JsonSerializer.Deserialize<Movie>(jsonResponse);

            var userId = _userManager.GetUserId(User);
            bool isFavorite = false;
            if (userId != null)
            {
                isFavorite = await _context.UserFavorites.AnyAsync(f => f.UserId == userId && f.MovieId == id);
            }
            ViewData["IsFavorite"] = isFavorite;

            return View(movie);
        }
    }
}