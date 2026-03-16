using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryTickets.Models
{
    public class SeatReservation
    {
        public int Id { get; set; }

        [Required]
        public int ShowtimeId { get; set; }

        [ForeignKey("ShowtimeId")]
        public Showtime Showtime { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Required]
        public string SeatNumber { get; set; }
    }
}
