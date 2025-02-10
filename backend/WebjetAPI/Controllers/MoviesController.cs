using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WebjetAPI.Models;
using WebjetAPI.Services;

namespace WebjetAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class MoviesController : ControllerBase
    {
        private readonly ILogger<MoviesController> _logger;
        private readonly IDatabase? _cache;

        public MoviesController(ILogger<MoviesController> logger, IConnectionMultiplexer redis)
        {
            _logger = logger;
            _cache = redis.GetDatabase();
        }

        /// <summary>
        /// Gets a consolidated list of all movies from both Cinemaworld and Filmworld, removing duplicates.
        /// </summary>
        [HttpGet("allmovies")]
        public async Task<IActionResult> GetAllMovies()
        {
            try
            {
                // Fetch cached movie lists from Redis
                var cinemaMovies = await GetMoviesFromCache("cinemaworld_movies");
                var filmMovies = await GetMoviesFromCache("filmworld_movies");

                // Dictionary to store unique movies by title
                var uniqueMovies = new Dictionary<string, Movie>();

                // Add Cinemaworld movies first (higher priority)
                foreach (var movie in cinemaMovies)
                {
                    if (!string.IsNullOrEmpty(movie.Title))
                    {
                        uniqueMovies[movie.Title] = movie;
                    }
                }

                // Add Filmworld movies, only if not already present
                foreach (var movie in filmMovies)
                {
                    if (!string.IsNullOrEmpty(movie.Title) && !uniqueMovies.ContainsKey(movie.Title))
                    {
                        uniqueMovies[movie.Title] = movie;
                    }
                }

                return Ok(uniqueMovies.Values.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all movies from cache");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Fetches a list of movies from Redis based on the given cache key.
        /// </summary>
        private async Task<List<Movie>> GetMoviesFromCache(string cacheKey)
        {
            if (_cache == null)
            {
                _logger.LogWarning("Redis cache is unavailable.");
                return new List<Movie>();
            }

            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (cachedData.IsNullOrEmpty)
            {
                _logger.LogWarning($"No data found in Redis for key: {cacheKey}");
                return new List<Movie>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<Movie>>(cachedData.ToString()) ?? new List<Movie>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to deserialize movie data for key: {cacheKey}");
                return new List<Movie>();
            }
        }
    }
}
