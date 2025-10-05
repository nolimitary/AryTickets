namespace AryTickets.Models
{
    public enum SeatStatus { Available, Taken, Selected }

    public class Seat
    {
        public string SeatNumber { get; set; }
        public SeatStatus Status { get; set; }
        public decimal Price { get; set; } = 12.50m; 
    }
}