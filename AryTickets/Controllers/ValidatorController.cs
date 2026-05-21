using AryTickets.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    // Box-office validator: staff scan a customer's QR code (live camera or uploaded
    // screenshot) and we resolve the booking server-side from its confirmation code.
    [Authorize(Roles = "Worker,Admin")]
    public class ValidatorController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ValidatorController(ApplicationDbContext db) => _db = db;

        public IActionResult Index() => View();

        // Accepts the raw scanned string. Our QR payload format is:
        //   ARYTIX-{confirmCode}|{productionTitle}|{performanceDateTime}|{seats}
        // We trust only the confirmation code — everything else comes back from the DB.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Validate([FromForm] string qrData)
        {
            if (string.IsNullOrWhiteSpace(qrData))
                return Json(new { ok = false, message = "Empty scan." });

            var trimmed = qrData.Trim();
            if (!trimmed.StartsWith("ARYTIX-", StringComparison.OrdinalIgnoreCase))
                return Json(new { ok = false, message = "Not an AryTix ticket." });

            var pipe = trimmed.IndexOf('|');
            var codeSegment = pipe > 0 ? trimmed.Substring(7, pipe - 7) : trimmed.Substring(7);
            var code = codeSegment.Trim().ToUpperInvariant();

            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.ConfirmationCode == code);
            if (booking == null)
                return Json(new { ok = false, message = "Ticket not found." });

            // Look up the actual show datetime so we can flag past / today / upcoming.
            DateTime? showTime = null;
            if (booking.PerformanceId.HasValue)
            {
                showTime = await _db.Performances
                    .Where(p => p.Id == booking.PerformanceId.Value)
                    .Select(p => (DateTime?)p.ShowDateTime)
                    .FirstOrDefaultAsync();
            }

            var status = "ok";
            var note = "Admit one.";
            if (showTime.HasValue)
            {
                var diff = showTime.Value - DateTime.UtcNow;
                if (diff.TotalMinutes < -180) { status = "past"; note = "This performance is already over."; }
                else if (diff.TotalMinutes > 720) { status = "early"; note = "This ticket is for a later date — check before admitting."; }
            }

            return Json(new
            {
                ok = true,
                status,
                note,
                code = booking.ConfirmationCode,
                production = booking.ProductionTitle,
                performance = booking.PerformanceDateTime,
                stage = booking.Stage,
                seats = booking.Seats,
                holder = booking.UserName,
                email = booking.UserEmail,
                bookedAt = booking.BookedAt.ToString("dd MMM yyyy · HH:mm")
            });
        }
    }
}
