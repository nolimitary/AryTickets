using System;
using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class Production
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string TitleOriginal { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Synopsis { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Playwright { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Director { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Cast { get; set; } = string.Empty;

        [MaxLength(60)]
        public string Genre { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        [MaxLength(500)]
        public string PosterUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string BackdropUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TrailerUrl { get; set; } = string.Empty;

        public DateTime PremiereDate { get; set; } = DateTime.UtcNow;

        public double Rating { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullPosterUrl =>
            !string.IsNullOrEmpty(PosterUrl)
                ? PosterUrl
                : "https://placehold.co/500x750/2b0a0a/d4af37?text=No+Poster";

        public string FullBackdropUrl =>
            !string.IsNullOrEmpty(BackdropUrl)
                ? BackdropUrl
                : "https://images.unsplash.com/photo-1503095396549-807759245b35?q=80&w=2400&auto=format&fit=crop";

        public string FormattedDuration =>
            DurationMinutes > 0
                ? $"{DurationMinutes / 60}ч {DurationMinutes % 60:D2}мин"
                : "—";

        public string RatingPercentage => $"{Rating * 10:F0}%";

        public string FormattedPremiere => PremiereDate.ToString("dd MMM yyyy");
    }
}
