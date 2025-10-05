using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class UserFavorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int MovieId { get; set; }

        public string MovieTitle { get; set; }

        public string PosterPath { get; set; }

        public string FullPosterPath => PosterPath != null ? $"https://image.tmdb.org/t/p/w500{PosterPath}" : "https://placehold.co/500x750/111827/FFFFFF?text=No+Image";
    }
}