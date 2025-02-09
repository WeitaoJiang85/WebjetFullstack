namespace WebjetAPI.Models;

/// <summary>
/// Represents a movie returned from the Cinemaworld and Filmworld APIs.
/// </summary>
public class Movie
{
    public string? Title { get; set; } = "Unknown";
    public string? Year { get; set; } = "Unknown";
    public string? ID { get; set; } = "N/A";
    public string? Type { get; set; } = "Unknown";
    public string Poster { get; set; } = "https://via.placeholder.com/300x450?text=No+Image";
}