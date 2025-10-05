using AryTickets.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AryTickets.Models;

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

        public async Task<IActionResult> Index(string region = "US")
        {
            ViewData["CurrentRegion"] = region;

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("TMDb API Key is not configured.");
                return View(new HomeViewModel { NowShowingMovies = new List<Movie>(), ComingSoonMovies = new List<Movie>() });
            }

            var httpClient = _httpClientFactory.CreateClient();

            var nowShowingUrl = $"https://api.themoviedb.org/3/movie/now_playing?api_key={_apiKey}&language=en-US&page=1&region={region}";
            var nowShowingResponse = await httpClient.GetAsync(nowShowingUrl);
            List<Movie> nowShowingMovies = new List<Movie>();
            if (nowShowingResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await nowShowingResponse.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResult>(jsonResponse);
                nowShowingMovies = apiResult?.Results ?? new List<Movie>();
            }

            var comingSoonUrl = $"https://api.themoviedb.org/3/movie/upcoming?api_key={_apiKey}&language=en-US&page=1&region={region}";
            var comingSoonResponse = await httpClient.GetAsync(comingSoonUrl);
            List<Movie> comingSoonMovies = new List<Movie>();
            if (comingSoonResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await comingSoonResponse.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResult>(jsonResponse);
                comingSoonMovies = apiResult?.Results ?? new List<Movie>();
            }

            var nowShowingIds = new HashSet<int>(nowShowingMovies.Select(m => m.Id));
            var filteredComingSoonMovies = comingSoonMovies.Where(m => !nowShowingIds.Contains(m.Id)).ToList();

            var viewModel = new HomeViewModel
            {
                NowShowingMovies = nowShowingMovies,
                ComingSoonMovies = filteredComingSoonMovies
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