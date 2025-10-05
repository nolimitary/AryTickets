using System.Collections.Generic;

namespace AryTickets.Models
{
    public class SeatSelectionViewModel
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        public string Showtime { get; set; }
        public List<List<Seat>> SeatingChart { get; set; }
    }
}