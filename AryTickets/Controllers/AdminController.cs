using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["TMDb:ApiKey"];
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalBookings = await _db.Bookings.CountAsync();
            var totalRevenue = (await _db.Bookings.Select(b => (double)b.TotalPrice).ToListAsync()).Sum();
            var totalFavorites = await _db.UserFavorites.CountAsync();
            var recentBookings = await _db.Bookings.OrderByDescending(b => b.BookedAt).Take(10).ToListAsync();
            var recentUsers = await _userManager.Users.OrderByDescending(u => u.Id).Take(5).ToListAsync();

            ViewData["TotalUsers"] = totalUsers;
            ViewData["TotalBookings"] = totalBookings;
            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["TotalFavorites"] = totalFavorites;
            ViewData["RecentBookings"] = recentBookings;
            ViewData["RecentUsers"] = recentUsers;

            return View();
        }

        // Users list
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<AdminUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = string.Join(", ", roles),
                    BookingCount = await _db.Bookings.CountAsync(b => b.UserId == user.Id)
                });
            }

            return View(userList);
        }

        // Toggle admin role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            else
                await _userManager.AddToRoleAsync(user, "Admin");

            return RedirectToAction(nameof(Users));
        }

        // Delete user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == userId) return RedirectToAction(nameof(Users));

            var userBookings = _db.Bookings.Where(b => b.UserId == userId);
            _db.Bookings.RemoveRange(userBookings);
            var userFavorites = _db.UserFavorites.Where(f => f.UserId == userId);
            _db.UserFavorites.RemoveRange(userFavorites);
            await _db.SaveChangesAsync();

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Users));
        }

        // Bookings list
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _db.Bookings.OrderByDescending(b => b.BookedAt).ToListAsync();
            return View(bookings);
        }

        // Delete booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking != null)
            {
                var reservations = _db.SeatReservations.Where(r => r.BookingId == id);
                _db.SeatReservations.RemoveRange(reservations);
                _db.Bookings.Remove(booking);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Bookings));
        }

        // Critic applications
        public async Task<IActionResult> Applications(string filter = "pending")
        {
            var query = _db.CriticApplications.Include(a => a.User).AsQueryable();

            if (filter == "pending")
                query = query.Where(a => a.Status == ApplicationStatus.Pending);
            else if (filter == "approved")
                query = query.Where(a => a.Status == ApplicationStatus.Approved);
            else if (filter == "denied")
                query = query.Where(a => a.Status == ApplicationStatus.Denied);

            var applications = await query.OrderByDescending(a => a.AppliedAt).ToListAsync();
            ViewData["CurrentFilter"] = filter;
            ViewData["PendingCount"] = await _db.CriticApplications.CountAsync(a => a.Status == ApplicationStatus.Pending);
            return View(applications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            var application = await _db.CriticApplications.FindAsync(id);
            if (application == null) return NotFound();

            application.Status = ApplicationStatus.Approved;
            application.ReviewedAt = System.DateTime.UtcNow;

            var user = await _userManager.FindByIdAsync(application.UserId);
            if (user != null)
                user.IsCritic = true;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Applications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyApplication(int id)
        {
            var application = await _db.CriticApplications.FindAsync(id);
            if (application == null) return NotFound();

            application.Status = ApplicationStatus.Denied;
            application.ReviewedAt = System.DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Applications));
        }

        // ═══ SHOWTIME MANAGEMENT ═══

        public async Task<IActionResult> Showtimes()
        {
            var showtimes = await _db.Showtimes
                .Where(s => s.IsActive)
                .OrderBy(s => s.MovieTitle)
                .ThenBy(s => s.ShowDateTime)
                .ToListAsync();

            return View(showtimes);
        }

        [HttpGet]
        public async Task<IActionResult> SearchMoviesApi(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            var httpClient = _httpClientFactory.CreateClient();
            var searchUrl = $"https://api.themoviedb.org/3/search/movie?api_key={_apiKey}&language=en-US&query={Uri.EscapeDataString(query)}&page=1";
            var response = await httpClient.GetAsync(searchUrl);

            if (!response.IsSuccessStatusCode)
                return Json(new List<object>());

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            var movies = (result?.Results ?? new List<Movie>())
                .Where(m => !string.IsNullOrEmpty(m.PosterPath))
                .Take(8)
                .Select(m => new
                {
                    id = m.Id,
                    title = m.Title,
                    posterPath = m.PosterPath,
                    fullPosterPath = m.FullPosterPath,
                    releaseDate = m.ReleaseDate,
                    voteAverage = m.VoteAverage
                });

            return Json(movies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShowtime(int tmdbMovieId, string movieTitle, string posterPath, string showtimes, string hall, decimal price)
        {
            if (string.IsNullOrWhiteSpace(showtimes) || string.IsNullOrWhiteSpace(movieTitle))
                return BadRequest();

            var dateTimeStrings = showtimes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var dtStr in dateTimeStrings)
            {
                if (DateTime.TryParse(dtStr.Trim(), out var showDateTime))
                {
                    _db.Showtimes.Add(new Showtime
                    {
                        TmdbMovieId = tmdbMovieId,
                        MovieTitle = movieTitle,
                        PosterPath = posterPath,
                        ShowDateTime = showDateTime,
                        Hall = hall ?? "Hall 1",
                        Price = price > 0 ? price : 12.50m
                    });
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Showtimes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShowtime(int id)
        {
            var showtime = await _db.Showtimes.FindAsync(id);
            if (showtime != null)
            {
                showtime.IsActive = false;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Showtimes));
        }

        // ═══ ANALYTICS ═══

        public async Task<IActionResult> Analytics()
        {
            var bookings = await _db.Bookings.ToListAsync();
            var now = DateTime.UtcNow;

            // Revenue over last 30 days
            var last30Days = Enumerable.Range(0, 30).Select(i => now.Date.AddDays(-29 + i)).ToList();
            var revenueByDate = bookings
                .Where(b => b.BookedAt.Date >= now.Date.AddDays(-29))
                .GroupBy(b => b.BookedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.TotalPrice));

            var revenueDates = last30Days.Select(d => d.ToString("MMM dd")).ToList();
            var revenueValues = last30Days.Select(d => revenueByDate.GetValueOrDefault(d, 0m)).ToList();

            // Top movies by booking count
            var topMovies = bookings
                .GroupBy(b => b.MovieTitle)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            // Bookings by day of week
            var dayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var bookingsByDay = bookings
                .GroupBy(b => b.BookedAt.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Count());
            var dayCounts = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                .Select(d => bookingsByDay.GetValueOrDefault(d, 0))
                .ToList();

            // Top movies by revenue
            var revenueByMovie = bookings
                .GroupBy(b => b.MovieTitle)
                .OrderByDescending(g => g.Sum(b => b.TotalPrice))
                .Take(8)
                .ToList();

            var model = new AnalyticsViewModel
            {
                RevenueDates = revenueDates,
                RevenueValues = revenueValues,
                TopMovieNames = topMovies.Select(g => g.Key.Length > 20 ? g.Key.Substring(0, 20) + "..." : g.Key).ToList(),
                TopMovieBookings = topMovies.Select(g => g.Count()).ToList(),
                DayNames = dayNames.ToList(),
                DayCounts = dayCounts,
                RevenueMovieNames = revenueByMovie.Select(g => g.Key.Length > 20 ? g.Key.Substring(0, 20) + "..." : g.Key).ToList(),
                RevenueMovieValues = revenueByMovie.Select(g => g.Sum(b => b.TotalPrice)).ToList(),
                TotalRevenue = bookings.Sum(b => b.TotalPrice),
                TotalBookings = bookings.Count,
                AverageOrderValue = bookings.Any() ? bookings.Average(b => b.TotalPrice) : 0,
                TodayRevenue = bookings.Where(b => b.BookedAt.Date == now.Date).Sum(b => b.TotalPrice)
            };

            return View(model);
        }
    }

    public class AdminUserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Roles { get; set; }
        public int BookingCount { get; set; }
    }
}
