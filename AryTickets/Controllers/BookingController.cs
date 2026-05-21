using AryTickets.Models;
using AryTickets.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using AryTickets.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Data.ApplicationDbContext _db;
        private readonly TicketPdfGenerator _pdfGenerator;
        private readonly IHubContext<SeatHub> _seatHub;
        private readonly StripeSettings _stripeSettings;

        public BookingController(IEmailSender emailSender, UserManager<ApplicationUser> userManager, Data.ApplicationDbContext db, TicketPdfGenerator pdfGenerator, IHubContext<SeatHub> seatHub, StripeSettings stripeSettings)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _db = db;
            _pdfGenerator = pdfGenerator;
            _seatHub = seatHub;
            _stripeSettings = stripeSettings;
        }

        public async Task<IActionResult> SelectSeats(int? performanceId)
        {
            if (!performanceId.HasValue)
                return BadRequest("A valid performance is required.");

            var perf = await _db.Performances
                .Include(p => p.Production)
                .FirstOrDefaultAsync(p => p.Id == performanceId.Value);
            if (perf == null || !perf.IsActive)
                return NotFound();

            if (perf.ShowDateTime <= System.DateTime.UtcNow)
                return BadRequest("This performance has already taken place.");

            var reservedSeatsList = await _db.SeatReservations
                .Where(r => r.PerformanceId == performanceId.Value)
                .Select(r => r.SeatNumber)
                .ToListAsync();
            var reservedSeats = new HashSet<string>(reservedSeatsList);

            var viewModel = new SeatSelectionViewModel
            {
                ProductionId = perf.ProductionId,
                ProductionTitle = perf.Production?.Title ?? string.Empty,
                PerformanceDateTime = perf.FormattedDateTime,
                PerformanceId = perf.Id,
                Stage = perf.Stage,
                TicketPrice = perf.Price,
                SeatingChart = GenerateSeatingChart(reservedSeats, perf.Price)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(string productionTitle, string performanceDateTime, string stage, string selectedSeats, decimal totalPrice, int? performanceId)
        {
            var viewModel = new CheckoutViewModel
            {
                ProductionTitle = productionTitle ?? string.Empty,
                PerformanceDateTime = performanceDateTime ?? string.Empty,
                Stage = stage ?? string.Empty,
                SelectedSeats = selectedSeats ?? string.Empty,
                TotalPrice = totalPrice,
                PerformanceId = performanceId
            };
            ViewData["StripeEnabled"] = _stripeSettings.IsConfigured;
            ViewData["StripePublishableKey"] = _stripeSettings.PublishableKey;
            return View(viewModel);
        }

        // Returns the Stripe publishable key to the browser so the Checkout view can
        // initialise Stripe Elements without ever exposing the secret key.
        [HttpGet]
        public IActionResult StripeConfig()
        {
            return Json(new
            {
                publishableKey = _stripeSettings.PublishableKey,
                configured = _stripeSettings.IsConfigured
            });
        }

        // Creates a Stripe PaymentIntent for the requested amount. The client confirms
        // it with the card data via Stripe.js; raw card data never reaches our server.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentIntent([FromForm] decimal amount, [FromForm] int? performanceId, [FromForm] string? selectedSeats)
        {
            if (!_stripeSettings.IsConfigured)
                return BadRequest(new { message = "Stripe is not configured." });

            if (amount <= 0)
                return BadRequest(new { message = "Invalid amount." });

            // Re-check seat availability before charging the customer.
            if (performanceId.HasValue && !string.IsNullOrEmpty(selectedSeats))
            {
                var seatNumbers = selectedSeats
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();

                var alreadyTaken = await _db.SeatReservations
                    .Where(r => r.PerformanceId == performanceId.Value && seatNumbers.Contains(r.SeatNumber))
                    .Select(r => r.SeatNumber)
                    .ToListAsync();

                if (alreadyTaken.Any())
                    return Conflict(new { message = $"Seats already taken: {string.Join(", ", alreadyTaken)}", takenSeats = alreadyTaken });
            }

            try
            {
                var service = new Stripe.PaymentIntentService();
                var intent = await service.CreateAsync(new Stripe.PaymentIntentCreateOptions
                {
                    // Stripe wants the amount in the smallest currency unit (stotinki for EUR).
                    Amount = (long)(amount * 100m),
                    Currency = "eur",
                    AutomaticPaymentMethods = new Stripe.PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["performanceId"] = performanceId?.ToString() ?? string.Empty,
                        ["seats"] = selectedSeats ?? string.Empty
                    }
                });

                return Json(new { clientSecret = intent.ClientSecret, paymentIntentId = intent.Id });
            }
            catch (Stripe.StripeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
        {
            // Stripe is mandatory — every booking must reference a real, succeeded PaymentIntent.
            if (!_stripeSettings.IsConfigured)
                return Json(new { success = false, message = "Payments are not configured. Please contact support." });

            if (string.IsNullOrWhiteSpace(model.StripePaymentIntentId))
                return Json(new { success = false, message = "Missing Stripe payment confirmation." });

            try
            {
                var intentService = new Stripe.PaymentIntentService();
                var intent = await intentService.GetAsync(model.StripePaymentIntentId);

                if (intent == null || intent.Status != "succeeded")
                    return Json(new { success = false, message = "Payment was not confirmed by Stripe." });

                var expectedAmount = (long)(model.TotalPrice * 100m);
                if (intent.Amount != expectedAmount)
                    return Json(new { success = false, message = "The amount does not match the confirmed payment." });
            }
            catch (Stripe.StripeException ex)
            {
                return Json(new { success = false, message = "Stripe error: " + ex.Message });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var confirmCode = System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var supportsTransactions = !_db.Database.ProviderName?.Contains("InMemory", System.StringComparison.OrdinalIgnoreCase) == true;
            var transaction = supportsTransactions ? await _db.Database.BeginTransactionAsync() : null;
            try
            {
                var seatNumbers = System.Array.Empty<string>();
                if (model.PerformanceId.HasValue && !string.IsNullOrEmpty(model.SelectedSeats))
                {
                    seatNumbers = model.SelectedSeats.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToArray();

                    var alreadyTaken = await _db.SeatReservations
                        .Where(r => r.PerformanceId == model.PerformanceId.Value && seatNumbers.Contains(r.SeatNumber))
                        .Select(r => r.SeatNumber)
                        .ToListAsync();

                    if (alreadyTaken.Any())
                    {
                        if (transaction != null) await transaction.RollbackAsync();
                        return Json(new { success = false, message = $"Seats already taken: {string.Join(", ", alreadyTaken)}", takenSeats = alreadyTaken });
                    }
                }

                var booking = new Booking
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    UserName = user.UserName,
                    ProductionTitle = model.ProductionTitle,
                    PerformanceDateTime = model.PerformanceDateTime,
                    Stage = model.Stage,
                    Seats = model.SelectedSeats,
                    TotalPrice = model.TotalPrice,
                    BookedAt = System.DateTime.UtcNow,
                    ConfirmationCode = confirmCode,
                    PerformanceId = model.PerformanceId
                };
                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();

                if (model.PerformanceId.HasValue && seatNumbers.Length > 0)
                {
                    foreach (var seat in seatNumbers)
                    {
                        _db.SeatReservations.Add(new SeatReservation
                        {
                            PerformanceId = model.PerformanceId.Value,
                            BookingId = booking.Id,
                            SeatNumber = seat
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                if (transaction != null) await transaction.CommitAsync();

                if (model.PerformanceId.HasValue && seatNumbers.Length > 0)
                {
                    await _seatHub.Clients.Group($"performance-{model.PerformanceId.Value}")
                        .SendAsync("SeatsBooked", seatNumbers);
                }

                try
                {
                    var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=ARYTIX-{confirmCode}|{model.ProductionTitle}|{model.PerformanceDateTime}|{model.SelectedSeats}";
                    var emailBody = BuildTicketEmail(model, user.UserName, confirmCode);

                    byte[] pdfBytes = null;
                    try
                    {
                        pdfBytes = _pdfGenerator.Generate(model.ProductionTitle, model.PerformanceDateTime, model.SelectedSeats, model.TotalPrice, confirmCode, qrUrl);
                    }
                    catch { }

                    if (pdfBytes != null)
                    {
                        await _emailSender.SendEmailWithAttachmentAsync(
                            user.Email,
                            "Your tickets for " + model.ProductionTitle,
                            emailBody,
                            pdfBytes,
                            $"AryTix-Bilet-{confirmCode}.pdf"
                        );
                    }
                    else
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Your tickets for " + model.ProductionTitle, emailBody);
                    }
                }
                catch
                {
                    // email failure should not roll back the booking
                }

                return Json(new { success = true, confirmationCode = booking.ConfirmationCode, qrCodeUrl = booking.QrCodeUrl });
            }
            catch (DbUpdateException)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return Json(new { success = false, message = "These seats were just booked by another viewer. Please choose different ones." });
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("BookingHistory", "Profile");
            }

            // Guard against cancelling shows that already happened or are within the
            // 2-hour box-office cutoff.
            if (booking.PerformanceId.HasValue)
            {
                var perf = await _db.Performances.FirstOrDefaultAsync(p => p.Id == booking.PerformanceId.Value);
                if (perf == null || perf.ShowDateTime <= System.DateTime.UtcNow.AddHours(2))
                {
                    TempData["ErrorMessage"] = "This booking can no longer be cancelled — the show is past or too close.";
                    return RedirectToAction("BookingHistory", "Profile");
                }
            }

            // Release seat reservations and broadcast so live seat-pickers refresh.
            var reservations = await _db.SeatReservations
                .Where(r => r.BookingId == booking.Id)
                .ToListAsync();
            var releasedSeats = reservations.Select(r => r.SeatNumber).ToArray();
            _db.SeatReservations.RemoveRange(reservations);
            _db.Bookings.Remove(booking);
            await _db.SaveChangesAsync();

            if (booking.PerformanceId.HasValue && releasedSeats.Length > 0)
            {
                await _seatHub.Clients.Group($"performance-{booking.PerformanceId.Value}")
                    .SendAsync("SeatsReleased", releasedSeats);
            }

            TempData["SuccessMessage"] = $"Booking cancelled. Seats {string.Join(", ", releasedSeats)} have been released.";
            return RedirectToAction("BookingHistory", "Profile");
        }

        private string BuildTicketEmail(CheckoutViewModel model, string username, string confirmCode)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='font-family: Georgia, \"Times New Roman\", serif; background-color: #1a0606; color: #f5e6d3; padding: 40px 20px; text-align: center;'>");
            sb.Append("<div style='max-width: 520px; margin: 0 auto;'>");
            sb.Append("<h1 style='font-size: 30px; letter-spacing: 0.18em; margin-bottom: 4px;'><span style='color: #f5e6d3; font-weight: 400;'>ARY</span><span style='color: #d4af37; font-weight: 400;'>TIX</span></h1>");
            sb.Append("<p style='color: #d4af37; font-size: 11px; letter-spacing: 0.28em; margin-bottom: 30px;'>T H E A T R I C A L &nbsp; S T A G E</p>");
            sb.Append("<div style='background-color: #2b0a0a; border: 1px solid rgba(212,175,55,0.25); border-radius: 6px; padding: 36px 32px; text-align: left; box-shadow: 0 8px 32px rgba(0,0,0,0.4);'>");
            sb.Append("<h2 style='color: #f5e6d3; font-size: 22px; margin: 0 0 6px 0; font-weight: 400;'>Booking confirmed</h2>");
            sb.AppendFormat("<p style='color: #c9a961; font-size: 14px; margin: 0 0 24px 0; font-style: italic;'>Dear {0}, we look forward to seeing you at the performance:</p>", username);
            sb.Append("<div style='background-color: #1a0606; border: 1px solid rgba(212,175,55,0.15); border-radius: 4px; padding: 22px; margin-bottom: 24px;'>");
            sb.Append("<table style='width: 100%; border-collapse: collapse;'>");
            sb.AppendFormat("<tr><td style='padding: 8px 0; color: #c9a961; font-size: 12px; letter-spacing: 0.1em;'>PRODUCTION</td><td style='padding: 8px 0; color: #f5e6d3; font-size: 15px; font-weight: 600; text-align: right;'>{0}</td></tr>", model.ProductionTitle);
            sb.AppendFormat("<tr><td style='padding: 8px 0; color: #c9a961; font-size: 12px; letter-spacing: 0.1em;'>DATE &amp; TIME</td><td style='padding: 8px 0; color: #f5e6d3; font-size: 14px; text-align: right;'>{0}</td></tr>", model.PerformanceDateTime);
            if (!string.IsNullOrEmpty(model.Stage))
                sb.AppendFormat("<tr><td style='padding: 8px 0; color: #c9a961; font-size: 12px; letter-spacing: 0.1em;'>STAGE</td><td style='padding: 8px 0; color: #f5e6d3; font-size: 14px; text-align: right;'>{0}</td></tr>", model.Stage);
            sb.AppendFormat("<tr><td style='padding: 8px 0; color: #c9a961; font-size: 12px; letter-spacing: 0.1em;'>SEATS</td><td style='padding: 8px 0; color: #f5e6d3; font-size: 14px; text-align: right;'>{0}</td></tr>", model.SelectedSeats);
            sb.Append("<tr><td colspan='2' style='padding: 12px 0 0 0;'><div style='border-top: 1px solid rgba(212,175,55,0.15);'></div></td></tr>");
            sb.AppendFormat("<tr><td style='padding: 12px 0 0 0; color: #c9a961; font-size: 12px; letter-spacing: 0.1em;'>TOTAL</td><td style='padding: 12px 0 0 0; color: #d4af37; font-size: 20px; font-weight: 700; text-align: right;'>{0:F2} EUR</td></tr>", model.TotalPrice);
            sb.Append("</table></div>");
            var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=ARYTIX-{confirmCode}|{model.ProductionTitle}|{model.PerformanceDateTime}|{model.SelectedSeats}";
            sb.Append("<div style='text-align: center; margin: 24px 0 16px;'>");
            sb.AppendFormat("<img src='{0}' alt='QR Code' style='border-radius: 4px; background: #f5e6d3; padding: 8px;' width='180' height='180' />", qrUrl);
            sb.AppendFormat("<p style='color: #c9a961; font-size: 11px; margin-top: 10px; letter-spacing: 0.2em;'>CODE: {0}</p>", confirmCode);
            sb.Append("</div>");
            sb.Append("<p style='color: #8b6914; font-size: 12px; text-align: center; margin: 0; font-style: italic;'>Show this QR code or confirmation at the theatre entrance.</p>");
            sb.Append("<p style='color: #8b6914; font-size: 12px; text-align: center; margin-top: 8px;'>A PDF ticket is attached to this email.</p>");
            sb.Append("</div>");
            sb.Append("<p style='color: #6b4f15; font-size: 11px; margin-top: 24px;'>&copy; 2026 AryTix · All rights reserved</p>");
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private List<List<Seat>> GenerateSeatingChart(HashSet<string> reservedSeats, decimal price)
        {
            var chart = new List<List<Seat>>();
            var rows = "ABCDEFGH".ToCharArray();
            for (int i = 0; i < rows.Length; i++)
            {
                var row = new List<Seat>();
                int seatCounter = 1;
                for (int j = 1; j <= 14; j++)
                {
                    if (j == 7 || j == 8) { row.Add(null); }
                    else if (rows[i] == 'H' && (j <= 2 || j >= 13)) { row.Add(null); }
                    else
                    {
                        var seatNum = $"{rows[i]}{seatCounter++}";
                        var status = reservedSeats.Contains(seatNum) ? SeatStatus.Taken : SeatStatus.Available;
                        row.Add(new Seat { SeatNumber = seatNum, Status = status, Price = price });
                    }
                }
                chart.Add(row);
            }
            return chart;
        }
    }
}
