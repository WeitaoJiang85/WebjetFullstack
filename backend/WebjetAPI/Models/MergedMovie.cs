namespace WebjetAPI.Models;

public class MergedMovie
{
    public string Title { get; set; } = "Unknown";
    public string Year { get; set; } = "Unknown";
    public string Type { get; set; } = "Unknown";
    public string Poster { get; set; } = "https://via.placeholder.com/300x450?text=No+Image";
    public List<string> IDs { get; set; } = new List<string>();
    public string RawID { get; set; } = string.Empty;
}