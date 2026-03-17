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

        public async Task<IActionResult> Index(string region = "US", int? genreId = null)
        {
            ViewData["CurrentRegion"] = region;

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("TMDb API Key is not configured.");
                return View(new HomeViewModel { NowShowingMovies = new List<Movie>(), ComingSoonMovies = new List<Movie>() });
            }

            var httpClient = _httpClientFactory.CreateClient();

            // Fetch genre list
            var genreUrl = $"https://api.themoviedb.org/3/genre/movie/list?api_key={_apiKey}&language=en-US";
            var genreResponse = await httpClient.GetAsync(genreUrl);
            var allGenres = new List<Genre>();
            if (genreResponse.IsSuccessStatusCode)
            {
                var genreJson = await genreResponse.Content.ReadAsStringAsync();
                var genreResult = JsonSerializer.Deserialize<GenreListResult>(genreJson);
                allGenres = genreResult?.Genres ?? new List<Genre>();
            }

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

            // Apply genre filter
            if (genreId.HasValue)
            {
                nowShowingMovies = nowShowingMovies.Where(m => m.GenreIds != null && m.GenreIds.Contains(genreId.Value)).ToList();
                filteredComingSoonMovies = filteredComingSoonMovies.Where(m => m.GenreIds != null && m.GenreIds.Contains(genreId.Value)).ToList();
            }

            var viewModel = new HomeViewModel
            {
                NowShowingMovies = nowShowingMovies,
                ComingSoonMovies = filteredComingSoonMovies,
                AllGenres = allGenres,
                SelectedGenreId = genreId
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

    public class GenreListResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("genres")]
        public List<Genre> Genres { get; set; }
    }
}
