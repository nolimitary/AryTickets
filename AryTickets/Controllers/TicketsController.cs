using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    // Customer-facing "My Tickets" page — focused on the upcoming-show QR codes.
    // The BookingHistory profile page still exists for past bookings and refunds.
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _db.Bookings
                .Where(b => b.UserId == userId)
                .OrderBy(b => b.BookedAt)
                .ToListAsync();

            var perfIds = bookings.Where(b => b.PerformanceId.HasValue)
                .Select(b => b.PerformanceId!.Value).Distinct().ToList();
            var perfTimes = await _db.Performances
                .Where(p => perfIds.Contains(p.Id))
                .Select(p => new { p.Id, p.ShowDateTime })
                .ToDictionaryAsync(p => p.Id, p => p.ShowDateTime);

            var items = bookings.Select(b => new BookingHistoryItem
            {
                Booking = b,
                ShowDateTime = b.PerformanceId.HasValue && perfTimes.TryGetValue(b.PerformanceId.Value, out var dt)
                    ? dt : (DateTime?)null
            })
            // Upcoming tickets first, sorted by show date — that's what users actually need on hand.
            .OrderBy(i => i.IsPast)
            .ThenBy(i => i.ShowDateTime ?? DateTime.MaxValue)
            .ToList();

            return View(items);
        }
    }
}
