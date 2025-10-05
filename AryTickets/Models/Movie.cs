using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AryTickets.Models
{
    public class Movie
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("overview")]
        public string Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string BackdropPath { get; set; }

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; }

        [JsonPropertyName("runtime")]
        public int? Runtime { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }

        [JsonPropertyName("genres")]
        public List<Genre> Genres { get; set; }

        [JsonPropertyName("credits")]
        public Credits Credits { get; set; }

        [JsonPropertyName("videos")]
        public VideoCollection Videos { get; set; }

        [JsonPropertyName("reviews")]
        public ReviewCollection Reviews { get; set; }

        public string FullPosterPath => PosterPath != null ? $"https://image.tmdb.org/t/p/w500{PosterPath}" : "https://placehold.co/500x750/111827/FFFFFF?text=No+Image";
        public string FullBackdropPath => BackdropPath != null ? $"https://image.tmdb.org/t/p/w1280{BackdropPath}" : "https://images.unsplash.com/photo-1594904351111-a072f80b1a71?q=80&w=2535&auto=format&fit=crop";
        public string FormattedRuntime => Runtime.HasValue ? $"{Runtime / 60}h {Runtime % 60}m" : "N/A";
        public string RatingPercentage => $"{VoteAverage * 10:F0}%";
        public Video FirstTrailer => Videos?.Results.FirstOrDefault(v => v.Type == "Trailer" && v.Site == "YouTube");
    }

    public class Genre { [JsonPropertyName("name")] public string Name { get; set; } }
    public class Credits { [JsonPropertyName("cast")] public List<CastMember> Cast { get; set; } }
    public class CastMember { [JsonPropertyName("name")] public string Name { get; set; } [JsonPropertyName("profile_path")] public string ProfilePath { get; set; } public string FullProfilePath => ProfilePath != null ? $"https://image.tmdb.org/t/p/w185{ProfilePath}" : "https://placehold.co/185x278/1F2937/FFFFFF?text=No+Photo"; }
    public class VideoCollection { [JsonPropertyName("results")] public List<Video> Results { get; set; } }
    public class Video { [JsonPropertyName("key")] public string Key { get; set; } [JsonPropertyName("site")] public string Site { get; set; } [JsonPropertyName("type")] public string Type { get; set; } public string YouTubeUrl => $"https://www.youtube.com/embed/{Key}"; }
    public class ReviewCollection { [JsonPropertyName("results")] public List<Review> Results { get; set; } }
    public class Review { [JsonPropertyName("author")] public string Author { get; set; } [JsonPropertyName("content")] public string Content { get; set; } [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; } [JsonPropertyName("author_details")] public AuthorDetails AuthorDetails { get; set; } }
    public class AuthorDetails { [JsonPropertyName("rating")] public double? Rating { get; set; } }
}