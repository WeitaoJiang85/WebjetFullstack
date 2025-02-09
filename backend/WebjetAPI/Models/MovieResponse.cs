namespace WebjetAPI.Models;

/// <summary>
/// Represents the response from Cinemaworld and Filmworld APIs.
/// </summary>
public class MovieResponse
{
    public List<Movie> Movies { get; set; } = new();
}
