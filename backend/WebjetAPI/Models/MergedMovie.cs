namespace WebjetAPI.Models;

/// <summary>
/// Represents a processed movie object returned by the API.
/// This model includes only the essential movie details.
/// </summary>
public class MergedMovie
{
    public string Title { get; set; } = "Unknown";
    public string Year { get; set; } = "Unknown";
    public string Poster { get; set; } = "https://via.placeholder.com/300x450?text=No+Image";
}
