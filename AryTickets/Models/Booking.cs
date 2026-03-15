using System;
using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public string UserEmail { get; set; }
        public string UserName { get; set; }

        [Required]
        public string MovieTitle { get; set; }

        public string Showtime { get; set; }
        public string Seats { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public string ConfirmationCode { get; set; }

        public string QrCodeUrl => $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=ARYTIX-{ConfirmationCode}|{MovieTitle}|{Showtime}|{Seats}";
    }
}