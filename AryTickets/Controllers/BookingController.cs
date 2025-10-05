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
                int seatCounter = 1; 

                for (int j = 1; j <= 14; j++)
                {
                    if (j == 7 || j == 8)
                    {
                        row.Add(null);
                    }
                  
                    else if (rows[i] == 'H' && (j <= 2 || j >= 13))
                    {
                        row.Add(null); 
                    }
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