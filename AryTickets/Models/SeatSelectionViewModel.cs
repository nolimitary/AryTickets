using System.Collections.Generic;

namespace AryTickets.Models
{
    public class SeatSelectionViewModel
    {
        public int ProductionId { get; set; }
        public string ProductionTitle { get; set; } = string.Empty;
        public string PerformanceDateTime { get; set; } = string.Empty;
        public int? PerformanceId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public decimal TicketPrice { get; set; } = 35.00m;

        // Flat list of seats with polar-derived (X, Y) coordinates — the view
        // renders them absolutely inside an SVG. Tests look up seats by SeatNumber.
        public List<Seat> Seats { get; set; } = new();
    }
}
