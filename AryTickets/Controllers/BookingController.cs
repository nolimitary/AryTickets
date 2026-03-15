using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AryTickets.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AryTickets.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(IEmailSender emailSender, UserManager<ApplicationUser> userManager)
        {
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult SelectSeats(int movieId, string movieTitle, string showtime)
        {
            if (string.IsNullOrEmpty(movieTitle) || string.IsNullOrEmpty(showtime))
            {
                return BadRequest("Movie and showtime information is required.");
            }

            var viewModel = new SeatSelectionViewModel
            {
                MovieId = movieId,
                MovieTitle = movieTitle,
                Showtime = showtime,
                SeatingChart = GenerateMockSeatingChart()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Checkout(string movieTitle, string showtime, string selectedSeats, decimal totalPrice)
        {
            var viewModel = new CheckoutViewModel
            {
                MovieTitle = movieTitle,
                Showtime = showtime,
                SelectedSeats = selectedSeats,
                TotalPrice = totalPrice
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid payment details.");
            }

            await Task.Delay(2500);

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var emailBody = BuildTicketEmail(model, user.UserName);
                await _emailSender.SendEmailAsync(user.Email, "Your Tickets for " + model.MovieTitle, emailBody);
            }

            return Json(new { success = true });
        }

        private string BuildTicketEmail(CheckoutViewModel model, string username)
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
            sb.Append("<p style='color: #52525b; font-size: 12px; text-align: center; margin: 0;'>Show this confirmation at the theater entrance.</p>");
            sb.Append("</div>");
            sb.Append("<p style='color: #3f3f46; font-size: 11px; margin-top: 24px;'>&copy; 2026 AryTix. All rights reserved.</p>");
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private List<List<Seat>> GenerateMockSeatingChart()
        {
            var chart = new List<List<Seat>>();
            var rows = "ABCDEFGH".ToCharArray();
            var random = new System.Random();
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
                        var status = random.Next(1, 10) > 8 ? SeatStatus.Taken : SeatStatus.Available;
                        row.Add(new Seat { SeatNumber = $"{rows[i]}{seatCounter++}", Status = status });
                    }
                }
                chart.Add(row);
            }
            return chart;
        }
    }
}