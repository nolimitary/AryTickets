using System.Text.Json.Serialization;

namespace YourAppName.Models;

public class ApiResult
{
    [JsonPropertyName("results")]
    public List<Movie> Results { get; set; }
}