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

        // Toggle admin role
        [HttpPost]
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
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Don't allow deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == userId) return RedirectToAction(nameof(Users));

            // Remove user's data
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
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking != null)
            {
                _db.Bookings.Remove(booking);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Bookings));
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