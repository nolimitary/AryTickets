using System;
using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class UserReview
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsCritic { get; set; }

        [Required]
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }

        [Range(1, 10)]
        public int Rating { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}