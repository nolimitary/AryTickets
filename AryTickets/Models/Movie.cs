// In YourAppName/Models/Movie.cs

using System.Text.Json.Serialization;

public class Movie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("poster_path")]
    public string PosterPath { get; set; }

    // 👇 ADD THIS PROPERTY! 👇
    [JsonPropertyName("overview")]
    public string Overview { get; set; }

    public string FullPosterPath => PosterPath != null ? $"https://image.tmdb.org/t/p/w500{PosterPath}" : "https://placehold.co/500x750/111827/FFFFFF?text=No+Image";
    public string FullBackdropPath => PosterPath != null ? $"https://image.tmdb.org/t/p/w1280{PosterPath}" : "https://images.unsplash.com/photo-1594904351111-a072f80b1a71?q=80&w=2535&auto=format&fit=crop";
}