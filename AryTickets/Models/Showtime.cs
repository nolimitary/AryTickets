using System;
using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class Showtime
    {
        public int Id { get; set; }

        [Required]
        public int TmdbMovieId { get; set; }

        [Required]
        public string MovieTitle { get; set; }

        public string PosterPath { get; set; }

        [Required]
        public DateTime ShowDateTime { get; set; }

        [Required]
        public string Hall { get; set; } = "Hall 1";

        public decimal Price { get; set; } = 12.50m;

        public int TotalSeats { get; set; } = 96;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullPosterPath => PosterPath != null
            ? $"https://image.tmdb.org/t/p/w500{PosterPath}"
            : "https://placehold.co/500x750/111827/FFFFFF?text=No+Image";

        public string FormattedDateTime => ShowDateTime.ToString("MMM dd, yyyy - h:mm tt");
        public string FormattedDate => ShowDateTime.ToString("MMM dd");
        public string FormattedTime => ShowDateTime.ToString("h:mm tt");
    }
}
