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

        public BookingController(IEmailSender emailSender, UserManager<ApplicationUser> userManager, Data.ApplicationDbContext db, TicketPdfGenerator pdfGenerator, IHubContext<SeatHub> seatHub)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _db = db;
            _pdfGenerator = pdfGenerator;
            _seatHub = seatHub;
        }

        public async Task<IActionResult> SelectSeats(int? showtimeId)
        {
            if (!showtimeId.HasValue)
                return BadRequest("A valid showtime is required.");

            var st = await _db.Showtimes.FindAsync(showtimeId.Value);
            if (st == null || !st.IsActive)
                return NotFound();

            // Don't allow booking past showtimes
            if (st.ShowDateTime <= System.DateTime.UtcNow)
                return BadRequest("This showtime has already passed.");

            // Get already reserved seats for this showtime
            var reservedSeatsList = await _db.SeatReservations
                .Where(r => r.ShowtimeId == showtimeId.Value)
                .Select(r => r.SeatNumber)
                .ToListAsync();
            var reservedSeats = new HashSet<string>(reservedSeatsList);

            var viewModel = new SeatSelectionViewModel
            {
                MovieId = st.TmdbMovieId,
                MovieTitle = st.MovieTitle,
                Showtime = st.FormattedDateTime,
                ShowtimeId = st.Id,
                Hall = st.Hall,
                TicketPrice = st.Price,
                SeatingChart = GenerateSeatingChart(reservedSeats, st.Price)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(string movieTitle, string showtime, string selectedSeats, decimal totalPrice, int? showtimeId)
        {
            var viewModel = new CheckoutViewModel
            {
                MovieTitle = movieTitle,
                Showtime = showtime,
                SelectedSeats = selectedSeats,
                TotalPrice = totalPrice,
                ShowtimeId = showtimeId
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
        {
            if (model.CardNumber != null)
                model.CardNumber = model.CardNumber.Replace(" ", "");

            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid payment details.");
            }

            await Task.Delay(2500);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var confirmCode = System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            // Use a transaction when the provider supports it (not InMemory)
            var supportsTransactions = !_db.Database.ProviderName?.Contains("InMemory", System.StringComparison.OrdinalIgnoreCase) == true;
            var transaction = supportsTransactions ? await _db.Database.BeginTransactionAsync() : null;
            try
            {
                // Check seat availability before booking
                var seatNumbers = System.Array.Empty<string>();
                if (model.ShowtimeId.HasValue && !string.IsNullOrEmpty(model.SelectedSeats))
                {
                    seatNumbers = model.SelectedSeats.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToArray();

                    var alreadyTaken = await _db.SeatReservations
                        .Where(r => r.ShowtimeId == model.ShowtimeId.Value && seatNumbers.Contains(r.SeatNumber))
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
                    MovieTitle = model.MovieTitle,
                    Showtime = model.Showtime,
                    Seats = model.SelectedSeats,
                    TotalPrice = model.TotalPrice,
                    BookedAt = System.DateTime.UtcNow,
                    ConfirmationCode = confirmCode,
                    ShowtimeId = model.ShowtimeId
                };
                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();

                // Create seat reservations
                if (model.ShowtimeId.HasValue && seatNumbers.Length > 0)
                {
                    foreach (var seat in seatNumbers)
                    {
                        _db.SeatReservations.Add(new SeatReservation
                        {
                            ShowtimeId = model.ShowtimeId.Value,
                            BookingId = booking.Id,
                            SeatNumber = seat
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                if (transaction != null) await transaction.CommitAsync();

                // Notify all browsers viewing this showtime that seats are now taken
                if (model.ShowtimeId.HasValue && seatNumbers.Length > 0)
                {
                    await _seatHub.Clients.Group($"showtime-{model.ShowtimeId.Value}")
                        .SendAsync("SeatsBooked", seatNumbers);
                }

                // Send confirmation email (non-blocking — don't fail the booking if email fails)
                try
                {
                    var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=ARYTIX-{confirmCode}|{model.MovieTitle}|{model.Showtime}|{model.SelectedSeats}";
                    var emailBody = BuildTicketEmail(model, user.UserName, confirmCode);

                    byte[] pdfBytes = null;
                    try
                    {
                        pdfBytes = _pdfGenerator.Generate(model.MovieTitle, model.Showtime, model.SelectedSeats, model.TotalPrice, confirmCode, qrUrl);
                    }
                    catch { }

                    if (pdfBytes != null)
                    {
                        await _emailSender.SendEmailWithAttachmentAsync(
                            user.Email,
                            "Your Tickets for " + model.MovieTitle,
                            emailBody,
                            pdfBytes,
                            $"AryTix-Ticket-{confirmCode}.pdf"
                        );
                    }
                    else
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Your Tickets for " + model.MovieTitle, emailBody);
                    }
                }
                catch
                {
                    // Email sending failed but payment still succeeds
                }

                return Json(new { success = true, confirmationCode = booking.ConfirmationCode, qrCodeUrl = booking.QrCodeUrl });
            }
            catch (DbUpdateException)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return Json(new { success = false, message = "Those seats were just booked by someone else. Please select different seats." });
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }

        private string BuildTicketEmail(CheckoutViewModel model, string username, string confirmCode)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='font-family: Arial, Helvetica, sans-serif; background-color: #09090b; color: #e4e4e7; padding: 40px 20px; text-align: center;'>");
            sb.Append("<div style='max-width: 500px; margin: 0 auto;'>");
            sb.Append("<h1 style='font-size: 26px; margin-bottom: 8px;'><span style='color: #fff; font-weight: 700;'>Ary</span><span style='color: #e11d48; font-weight: 300;'>Tix</span></h1>");
            sb.Append("<p style='color: #71717a; font-size: 13px; margin-bottom: 30px;'>Your ticket confirmation</p>");
            sb.Append("<div style='background-color: #141416; border-radius: 16px; padding: 32px; text-align: left; border: 1px solid rgba(255,255,255,0.06);'>");
            sb.Append("<h2 style='color: #fff; font-size: 20px; margin: 0 0 6px 0;'>Booking Confirmed!</h2>");
            sb.AppendFormat("<p style='color: #a1a1aa; font-size: 14px; margin: 0 0 24px 0;'>Hi {0}, here are your ticket details:</p>", username);
            sb.Append("<div style='background-color: #09090b; border-radius: 12px; padding: 20px; margin-bottom: 24px;'>");
            sb.Append("<table style='width: 100%; border-collapse: collapse;'>");
            sb.AppendFormat("<tr><td style='padding: 8px 0; color: #71717a; font-size: 13px;'>Movie</td><td style='padding: 8px 0; color: #fff; font-size: 14px; font-weight: 600; text-align: right;'>{0}</td></tr>", model.MovieTitle);
            sb.AppendFormat("<tr><td style='padding: 8px 0; color: #71717a; font-size: 13px;'>Showtime</td><td style='padding: 8px 0; color: #fff; font-size: 14px; text-align: right;'>{0}</td></tr>", model.Showtime);
            sb.AppendFormat("<tr><td style='padding: 8px 0; color: #71717a; font-size: 13px;'>Seats</td><td style='padding: 8px 0; color: #fff; font-size: 14px; text-align: right;'>{0}</td></tr>", model.SelectedSeats);
            sb.Append("<tr><td colspan='2' style='padding: 12px 0 0 0;'><div style='border-top: 1px solid rgba(255,255,255,0.06);'></div></td></tr>");
            sb.AppendFormat("<tr><td style='padding: 12px 0 0 0; color: #71717a; font-size: 13px;'>Total</td><td style='padding: 12px 0 0 0; color: #e11d48; font-size: 18px; font-weight: 700; text-align: right;'>${0:F2}</td></tr>", model.TotalPrice);
            sb.Append("</table></div>");
            var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=ARYTIX-{confirmCode}|{model.MovieTitle}|{model.Showtime}|{model.SelectedSeats}";
            sb.Append("<div style='text-align: center; margin: 24px 0 16px;'>");
            sb.AppendFormat("<img src='{0}' alt='QR Code' style='border-radius: 8px;' width='180' height='180' />", qrUrl);
            sb.AppendFormat("<p style='color: #71717a; font-size: 11px; margin-top: 8px; letter-spacing: 0.1em;'>CODE: {0}</p>", confirmCode);
            sb.Append("</div>");
            sb.Append("<p style='color: #52525b; font-size: 12px; text-align: center; margin: 0;'>Scan the QR code or show this confirmation at the theater entrance.</p>");
            sb.Append("<p style='color: #52525b; font-size: 12px; text-align: center; margin-top: 8px;'>A PDF ticket is attached to this email.</p>");
            sb.Append("</div>");
            sb.Append("<p style='color: #3f3f46; font-size: 11px; margin-top: 24px;'>&copy; 2026 AryTix. All rights reserved.</p>");
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
