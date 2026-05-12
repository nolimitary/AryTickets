using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryTickets.Models
{
    public class UserFavorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int ProductionId { get; set; }

        [ForeignKey("ProductionId")]
        public Production Production { get; set; }

        public string ProductionTitle { get; set; }

        public string PosterUrl { get; set; }

        public string FullPosterUrl =>
            !string.IsNullOrEmpty(PosterUrl)
                ? PosterUrl
                : "https://placehold.co/500x750/2b0a0a/d4af37?text=No+Poster";
    }
}
