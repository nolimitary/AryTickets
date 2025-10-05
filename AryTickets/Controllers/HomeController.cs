using AryTickets.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using YourAppName.Models; 

namespace AryTickets.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["TMDb:ApiKey"]; 
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("TMDb API Key is not configured.");
                var emptyViewModel = new HomeViewModel
                {
                    NowShowingMovies = new List<Movie>(),
                    ComingSoonMovies = new List<Movie>()
                };
                return View(emptyViewModel);
            }

            var httpClient = _httpClientFactory.CreateClient();

            var nowShowingUrl = $"https://api.themoviedb.org/3/movie/now_playing?api_key={_apiKey}&language=en-US&page=1";
            var nowShowingResponse = await httpClient.GetAsync(nowShowingUrl);
            List<Movie> nowShowingMovies = new();
            if (nowShowingResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await nowShowingResponse.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResult>(jsonResponse);
                nowShowingMovies = apiResult?.Results ?? new List<Movie>();
            }
            else
            {
                _logger.LogError("Failed to fetch 'Now Showing' movies from TMDb.");
            }

           var comingSoonUrl = $"https://api.themoviedb.org/3/movie/upcoming?api_key={_apiKey}&language=en-US&page=1";
            var comingSoonResponse = await httpClient.GetAsync(comingSoonUrl);
            List<Movie> comingSoonMovies = new();
            if (comingSoonResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await comingSoonResponse.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResult>(jsonResponse);
                comingSoonMovies = apiResult?.Results ?? new List<Movie>();
            }
            else
            {
                _logger.LogError("Failed to fetch 'Coming Soon' movies from TMDb.");
            }

            var viewModel = new HomeViewModel
            {
                NowShowingMovies = nowShowingMovies,
                ComingSoonMovies = comingSoonMovies
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}