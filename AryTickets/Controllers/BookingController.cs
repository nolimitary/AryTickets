using AryTickets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AryTickets.Controllers
{
    [Authorize] 
    public class BookingController : Controller
    {
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

        private List<List<Seat>> GenerateMockSeatingChart()
        {
            var chart = new List<List<Seat>>();
            var rows = "ABCDEFGH".ToCharArray();
            var random = new System.Random();

            for (int i = 0; i < rows.Length; i++)
            {
                var row = new List<Seat>();
                for (int j = 1; j <= 12; j++)
                {
                    var status = random.Next(1, 10) > 7 ? SeatStatus.Taken : SeatStatus.Available;
                    if (j == 3 || j == 10)
                    {
                        row.Add(null); 
                    }
                    else
                    {
                        row.Add(new Seat { SeatNumber = $"{rows[i]}{j}", Status = status });
                    }
                }
                chart.Add(row);
            }
            return chart;
        }
    }
}