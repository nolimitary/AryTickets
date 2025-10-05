using AryTickets.Models;
using AryTickets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;

        public BookingController(IEmailSender emailSender, UserManager<IdentityUser> userManager)
        {
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult SelectSeats(int movieId)
        {
            var viewModel = new SeatSelectionViewModel
            {
                MovieId = movieId,
                MovieTitle = "The Epic Movie",
                Showtime = "October 26, 2025 - 8:00 PM",
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
            sb.Append("<div style='font-family: Poppins, sans-serif; background-color: #111827; color: #F3F4F6; padding: 40px; text-align: center;'>");
            sb.Append("<h1 style='color: #6366F1; font-size: 28px;'>AryTix</h1>");
            sb.Append("<div style='background-color: #1F2937; border-radius: 12px; padding: 30px; margin: 20px auto; max-width: 500px; text-align: left;'>");
            sb.Append("<h2 style='color: white; font-size: 24px; border-bottom: 1px solid #374151; padding-bottom: 15px;'>Your Booking is Confirmed!</h2>");
            sb.AppendFormat("<p style='color: #D1D5DB; margin-top: 20px;'>Hi {0}, here are your ticket details:</p>", username);
            sb.Append("<div style='margin-top: 25px;'>");
            sb.AppendFormat("<p style='margin: 10px 0;'><strong style='color: #9CA3AF;'>Movie:</strong> {0}</p>", model.MovieTitle);
            sb.AppendFormat("<p style='margin: 10px 0;'><strong style='color: #9CA3AF;'>Showtime:</strong> {0}</p>", model.Showtime);
            sb.AppendFormat("<p style='margin: 10px 0;'><strong style='color: #9CA3AF;'>Seats:</strong> {0}</p>", model.SelectedSeats);
            sb.AppendFormat("<p style='margin: 10px 0;'><strong style='color: #9CA3AF;'>Total:</strong> <span style='color: #818CF8; font-weight: bold;'>${0:F2}</span></p>", model.TotalPrice);
            sb.Append("</div>");
            sb.Append("<div style='margin-top: 30px; padding-top: 20px; border-top: 1px dashed #4B5563; text-align: center;'>");
            sb.Append("<p style='color: #9CA3AF; font-size: 12px;'>Please show this confirmation at the theater.</p>");
            sb.Append("</div></div></div>");
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