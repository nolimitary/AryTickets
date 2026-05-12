using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryTickets.Models
{
    public class Performance
    {
        public int Id { get; set; }

        [Required]
        public int ProductionId { get; set; }

        [ForeignKey("ProductionId")]
        public Production Production { get; set; }

        [Required]
        public DateTime ShowDateTime { get; set; }

        [Required]
        [MaxLength(100)]
        public string Stage { get; set; } = "Голяма сцена";

        public decimal Price { get; set; } = 35.00m;

        public int TotalSeats { get; set; } = 96;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FormattedDateTime => ShowDateTime.ToString("dd MMM yyyy · HH:mm");
        public string FormattedDate => ShowDateTime.ToString("dd MMM");
        public string FormattedTime => ShowDateTime.ToString("HH:mm");
    }
}
