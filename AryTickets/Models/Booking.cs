using System;
using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string ProductionTitle { get; set; } = string.Empty;

        public string PerformanceDateTime { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public string Seats { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public string ConfirmationCode { get; set; } = string.Empty;

        public int? PerformanceId { get; set; }

        public string QrCodeUrl =>
            $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=ARYTIX-{ConfirmationCode}|{ProductionTitle}|{PerformanceDateTime}|{Seats}";
    }
}
