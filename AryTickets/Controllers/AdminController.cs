using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
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
            application.ReviewedAt = DateTime.UtcNow;

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
            application.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Applications));
        }

        // ═══ PRODUCTION MANAGEMENT ═══

        public async Task<IActionResult> Productions()
        {
            var productions = await _db.Productions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Title)
                .ToListAsync();
            return View(productions);
        }

        [HttpGet]
        public IActionResult CreateProduction()
        {
            return View(new Production());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduction(Production model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;
            _db.Productions.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Productions));
        }

        [HttpGet]
        public async Task<IActionResult> EditProduction(int id)
        {
            var production = await _db.Productions.FindAsync(id);
            if (production == null) return NotFound();
            return View(production);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduction(Production model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _db.Productions.FindAsync(model.Id);
            if (existing == null) return NotFound();

            existing.Title = model.Title;
            existing.TitleOriginal = model.TitleOriginal;
            existing.Synopsis = model.Synopsis;
            existing.Playwright = model.Playwright;
            existing.Director = model.Director;
            existing.Cast = model.Cast;
            existing.Genre = model.Genre;
            existing.DurationMinutes = model.DurationMinutes;
            existing.PosterUrl = model.PosterUrl;
            existing.BackdropUrl = model.BackdropUrl;
            existing.TrailerUrl = model.TrailerUrl;
            existing.PremiereDate = model.PremiereDate;
            existing.Rating = model.Rating;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Productions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduction(int id)
        {
            var production = await _db.Productions.FindAsync(id);
            if (production != null)
            {
                production.IsActive = false;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Productions));
        }

        // ═══ PERFORMANCE MANAGEMENT ═══

        public async Task<IActionResult> Performances()
        {
            var performances = await _db.Performances
                .Include(p => p.Production)
                .Where(p => p.IsActive)
                .OrderBy(p => p.ShowDateTime)
                .ToListAsync();

            ViewData["Productions"] = await _db.Productions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Title)
                .ToListAsync();

            return View(performances);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePerformance(int productionId, string performances, string stage, decimal price)
        {
            if (string.IsNullOrWhiteSpace(performances) || productionId <= 0)
                return BadRequest();

            var production = await _db.Productions.FindAsync(productionId);
            if (production == null) return NotFound();

            var dateTimeStrings = performances.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var dtStr in dateTimeStrings)
            {
                if (DateTime.TryParse(dtStr.Trim(), out var showDateTime))
                {
                    _db.Performances.Add(new Performance
                    {
                        ProductionId = productionId,
                        ShowDateTime = showDateTime,
                        Stage = string.IsNullOrWhiteSpace(stage) ? "Голяма сцена" : stage,
                        Price = price > 0 ? price : 35.00m
                    });
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Performances));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePerformance(int id)
        {
            var performance = await _db.Performances.FindAsync(id);
            if (performance != null)
            {
                performance.IsActive = false;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Performances));
        }

        // ═══ ANALYTICS ═══

        public async Task<IActionResult> Analytics()
        {
            var bookings = await _db.Bookings.ToListAsync();
            var now = DateTime.UtcNow;

            var last30Days = Enumerable.Range(0, 30).Select(i => now.Date.AddDays(-29 + i)).ToList();
            var revenueByDate = bookings
                .Where(b => b.BookedAt.Date >= now.Date.AddDays(-29))
                .GroupBy(b => b.BookedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.TotalPrice));

            var revenueDates = last30Days.Select(d => d.ToString("dd MMM")).ToList();
            var revenueValues = last30Days.Select(d => revenueByDate.GetValueOrDefault(d, 0m)).ToList();

            var topProductions = bookings
                .GroupBy(b => b.ProductionTitle)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            var dayNames = new[] { "Пон", "Вто", "Сря", "Чет", "Пет", "Съб", "Нед" };
            var bookingsByDay = bookings
                .GroupBy(b => b.BookedAt.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Count());
            var dayCounts = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
                .Select(d => bookingsByDay.GetValueOrDefault(d, 0))
                .ToList();

            var revenueByProduction = bookings
                .GroupBy(b => b.ProductionTitle)
                .OrderByDescending(g => g.Sum(b => b.TotalPrice))
                .Take(8)
                .ToList();

            var model = new AnalyticsViewModel
            {
                RevenueDates = revenueDates,
                RevenueValues = revenueValues,
                TopProductionNames = topProductions.Select(g => g.Key.Length > 22 ? g.Key.Substring(0, 22) + "..." : g.Key).ToList(),
                TopProductionBookings = topProductions.Select(g => g.Count()).ToList(),
                DayNames = dayNames.ToList(),
                DayCounts = dayCounts,
                RevenueProductionNames = revenueByProduction.Select(g => g.Key.Length > 22 ? g.Key.Substring(0, 22) + "..." : g.Key).ToList(),
                RevenueProductionValues = revenueByProduction.Select(g => g.Sum(b => b.TotalPrice)).ToList(),
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
