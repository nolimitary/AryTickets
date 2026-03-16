using System.Collections.Generic;

namespace AryTickets.Models
{
    public class SeatSelectionViewModel
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        public string Showtime { get; set; }
        public int? ShowtimeId { get; set; }
        public string Hall { get; set; }
        public decimal TicketPrice { get; set; } = 12.50m;
        public List<List<Seat>> SeatingChart { get; set; }
    }
}