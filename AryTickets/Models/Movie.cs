using System.Text.Json.Serialization;

namespace YourAppName.Models;

public class Movie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("poster_path")]
    public string PosterPath { get; set; }

    [JsonPropertyName("overview")]
    public string Overview { get; set; }

    public string FullPosterPath => $"https://image.tmdb.org/t/p/w500{PosterPath}";
}