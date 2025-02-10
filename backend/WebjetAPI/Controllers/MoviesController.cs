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

    /// <summary>
    /// Get the top 3 latest movies, merging data from both providers.
    /// </summary>
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

    /// <summary>
    /// Get all merged movies from both providers.
    /// </summary>
    [HttpGet("allMergedMovies")]
    public async Task<IActionResult> GetMergedMovies()
    {
        try
        {
            var cinemaData = await _cache.StringGetAsync("cinemaworld_movies");
            var filmData = await _cache.StringGetAsync("filmworld_movies");

            var cinemaMovies = !string.IsNullOrEmpty(cinemaData) 
                ? JsonSerializer.Deserialize<List<MovieDetail>>(cinemaData) ?? new List<MovieDetail>() 
                : new List<MovieDetail>();

            var filmMovies = !string.IsNullOrEmpty(filmData) 
                ? JsonSerializer.Deserialize<List<MovieDetail>>(filmData) ?? new List<MovieDetail>() 
                : new List<MovieDetail>();

            var mergedMovies = MergeMovieDetails(cinemaMovies, filmMovies);
            return Ok(mergedMovies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging movie details.");
            return StatusCode(500, "Error merging movie details.");
        }
    }

    /// <summary>
    /// Merges movie details from both providers.
    /// </summary>
    private List<MergedMovieDetail> MergeMovieDetails(List<MovieDetail> cinemaMovies, List<MovieDetail> filmMovies)
    {
        var movieDictionary = new Dictionary<string, MergedMovieDetail>();

        foreach (var movie in cinemaMovies)
        {
            movieDictionary[movie.ID] = new MergedMovieDetail
            {
                Title = movie.Title,
                Year = movie.Year,
                Rated = movie.Rated,
                Released = movie.Released,
                Runtime = movie.Runtime,
                Genre = movie.Genre,
                Director = movie.Director,
                Writer = movie.Writer,
                Actors = movie.Actors,
                Plot = movie.Plot,
                Language = movie.Language,
                Country = movie.Country,
                Awards = movie.Awards,
                Poster = movie.Poster,
                Metascore = movie.Metascore,
                Rating = decimal.TryParse(movie.Rating, out var rating) ? rating : 0.0m,
                Votes = int.TryParse(movie.Votes.Replace(",", ""), out var votes) ? votes : 0,
                ID = movie.ID,
                Type = movie.Type,
                FirstPrice = movie.Price,
                FirstProvider = "Cinemaworld",
                SecondPrice = -1m,
                SecondProvider = "Unknown"
            };
        }

        foreach (var movie in filmMovies)
        {
            if (movieDictionary.TryGetValue(movie.ID, out var existingMovie))
            {
                if (movie.Price < existingMovie.FirstPrice)
                {
                    existingMovie.SecondPrice = existingMovie.FirstPrice;
                    existingMovie.SecondProvider = existingMovie.FirstProvider;
                    existingMovie.FirstPrice = movie.Price;
                    existingMovie.FirstProvider = "Filmworld";
                }
                else
                {
                    existingMovie.SecondPrice = movie.Price;
                    existingMovie.SecondProvider = "Filmworld";
                }

                var newRating = decimal.TryParse(movie.Rating, out var rating) ? rating : 0.0m;
                existingMovie.Rating = (existingMovie.Rating + newRating) / 2;

                var newVotes = int.TryParse(movie.Votes.Replace(",", ""), out var votes) ? votes : 0;
                existingMovie.Votes += newVotes;
            }
            else
            {
                movieDictionary[movie.ID] = new MergedMovieDetail
                {
                    Title = movie.Title,
                    Year = movie.Year,
                    Rated = movie.Rated,
                    Released = movie.Released,
                    Runtime = movie.Runtime,
                    Genre = movie.Genre,
                    Director = movie.Director,
                    Writer = movie.Writer,
                    Actors = movie.Actors,
                    Plot = movie.Plot,
                    Language = movie.Language,
                    Country = movie.Country,
                    Awards = movie.Awards,
                    Poster = movie.Poster,
                    Metascore = movie.Metascore,
                    Rating = decimal.TryParse(movie.Rating, out var rating) ? rating : 0.0m,
                    Votes = int.TryParse(movie.Votes.Replace(",", ""), out var votes) ? votes : 0,
                    ID = movie.ID,
                    Type = movie.Type,
                    FirstPrice = movie.Price,
                    FirstProvider = "Filmworld",
                    SecondPrice = -1m,
                    SecondProvider = "Unknown"
                };
            }
        }

        return movieDictionary.Values.ToList();
    }
}
