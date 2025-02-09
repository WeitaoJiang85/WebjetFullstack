using System.Text.Json.Serialization;
using WebjetAPI.Utilities;

namespace WebjetAPI.Models;

/// <summary>
/// Represents detailed movie information from the provider API.
/// </summary>
public class MovieDetail
{
    public string Title { get; set; } = "Unknown";
    public string Year { get; set; } = "Unknown";
    public string Rated { get; set; } = "N/A";
    public string Released { get; set; } = "N/A";
    public string Runtime { get; set; } = "N/A";
    public string Genre { get; set; } = "Unknown";
    public string Director { get; set; } = "Unknown";
    public string Writer { get; set; } = "Unknown";
    public string Actors { get; set; } = "Unknown";
    public string Plot { get; set; } = "N/A";
    public string Language { get; set; } = "Unknown";
    public string Country { get; set; } = "Unknown";
    public string Awards { get; set; } = "N/A";
    public string Poster { get; set; } = "https://via.placeholder.com/300x450?text=No+Image";
    public string Metascore { get; set; } = "N/A";
    public string Rating { get; set; } = "N/A";
    public string Votes { get; set; } = "N/A";
    public string ID { get; set; } = "N/A";
    public string Type { get; set; } = "Unknown";

    [JsonPropertyName("Price")]
    [JsonConverter(typeof(JsonStringConverterForDecimal))]
    public decimal Price { get; set; } = 0.0m;

    public string Provider { get; set; } = "Unknown";
}
