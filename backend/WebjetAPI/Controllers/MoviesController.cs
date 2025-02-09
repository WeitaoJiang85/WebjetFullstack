using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WebjetAPI.Models;

namespace WebjetAPI.Controllers;

/// <summary>
/// API controller for retrieving movie data.
/// </summary>
[Route("api/movies")]
[ApiController]
public class MoviesController : ControllerBase
{
    private readonly ILogger<MoviesController> _logger;
    private readonly IDatabase _cache;

    public MoviesController(ILogger<MoviesController> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _cache = redis.GetDatabase();
    }

    [HttpGet("top3")]
    public async Task<IActionResult> GetLatestMovies()
    {
        try
        {
            var allMovies = new List<MovieDetail>();

            // Fetch Cinemaworld movie details
            var cinemaKeys = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints()[0])
                              .Keys(pattern: "cinemaworld_movie_details:*")
                              .ToList();
            foreach (var key in cinemaKeys)
            {
                var data = await _cache.StringGetAsync(key);
                if (!string.IsNullOrEmpty(data))
                {
                    var movie = JsonSerializer.Deserialize<MovieDetail>(data);
                    if (movie != null) allMovies.Add(movie);
                }
            }

            // Fetch Filmworld movie details
            var filmKeys = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints()[0])
                              .Keys(pattern: "filmworld_movie_details:*")
                              .ToList();
            foreach (var key in filmKeys)
            {
                var data = await _cache.StringGetAsync(key);
                if (!string.IsNullOrEmpty(data))
                {
                    var movie = JsonSerializer.Deserialize<MovieDetail>(data);
                    if (movie != null) allMovies.Add(movie);
                }
            }

            if (!allMovies.Any())
            {
                _logger.LogWarning("No movie data available.");
                return NotFound("No movie data available.");
            }

            // Merge movies from both providers, keeping the lowest price if duplicate
            var mergedMovies = allMovies
                .GroupBy(m => m.Title)
                .Select(group =>
                {
                    var bestMovie = group.OrderBy(m => m.Price).First();
                    return new MovieDetail
                    {
                        Title = bestMovie.Title,
                        Year = bestMovie.Year,
                        Rated = bestMovie.Rated,
                        Released = bestMovie.Released,
                        Runtime = bestMovie.Runtime,
                        Genre = bestMovie.Genre,
                        Director = bestMovie.Director,
                        Writer = bestMovie.Writer,
                        Actors = bestMovie.Actors,
                        Plot = bestMovie.Plot,
                        Language = bestMovie.Language,
                        Country = bestMovie.Country,
                        Awards = bestMovie.Awards,
                        Poster = bestMovie.Poster,
                        Metascore = bestMovie.Metascore,
                        Rating = bestMovie.Rating,
                        Votes = bestMovie.Votes,
                        ID = bestMovie.ID,
                        Type = bestMovie.Type,
                        Price = bestMovie.Price,
                        Provider = bestMovie.Provider
                    };
                })
                .OrderByDescending(m => int.Parse(m.Year))
                .ThenBy(m => m.Title)
                .Take(3)
                .ToList();

            _logger.LogInformation($"Returning top {mergedMovies.Count} latest movies.");
            return Ok(mergedMovies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movie data.");
            return StatusCode(500, "Error fetching movie data.");
        }
    }
}
