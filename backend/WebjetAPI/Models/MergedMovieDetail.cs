using System.Text.Json.Serialization;
using WebjetAPI.Utilities;

namespace WebjetAPI.Models;

/// <summary>
/// Represents merged movie information from multiple providers.
/// </summary>
public class MergedMovieDetail
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

    [JsonPropertyName("Rating")]
    public decimal Rating { get; set; } = 0.0m;

    [JsonPropertyName("Votes")]
    public int Votes { get; set; } = 0;

    public string ID { get; set; } = "N/A";
    public string Type { get; set; } = "Unknown";

    [JsonPropertyName("FirstPrice")]
    [JsonConverter(typeof(JsonStringConverterForDecimal))]
    public decimal FirstPrice { get; set; } = -1m; // -1m indicates missing value

    public string FirstProvider { get; set; } = "Unknown";

    [JsonPropertyName("SecondPrice")]
    [JsonConverter(typeof(JsonStringConverterForDecimal))]
    public decimal SecondPrice { get; set; } = -1m; // -1m indicates missing value

    public string SecondProvider { get; set; } = "Unknown";
}
