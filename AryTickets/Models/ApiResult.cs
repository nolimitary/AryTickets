using System.Text.Json.Serialization;

namespace AryTickets.Models;

public class ApiResult
{
    [JsonPropertyName("results")]
    public List<Movie> Results { get; set; }
}