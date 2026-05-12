using System.Collections.Generic;

namespace AryTickets.Models
{
    public class SeatSelectionViewModel
    {
        public int ProductionId { get; set; }
        public string ProductionTitle { get; set; }
        public string PerformanceDateTime { get; set; }
        public int? PerformanceId { get; set; }
        public string Stage { get; set; }
        public decimal TicketPrice { get; set; } = 35.00m;
        public List<List<Seat>> SeatingChart { get; set; }
    }
}
