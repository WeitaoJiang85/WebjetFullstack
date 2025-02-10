using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using WebjetAPI.Models;

namespace WebjetAPI.Controllers
{
    /// <summary>
    /// API controller for retrieving merged movie details.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class MergedMoviesController : ControllerBase
    {
        private readonly ILogger<MergedMoviesController> _logger;
        private readonly IDatabase? _cache;

        public MergedMoviesController(ILogger<MergedMoviesController> logger, IConnectionMultiplexer redis)
        {
            _logger = logger;
            _cache = redis.GetDatabase();
        }

        /// <summary>
        /// Retrieves the merged movie details from Redis cache.
        /// </summary>
        [HttpGet("mergedmoviedetails")]
        public async Task<IActionResult> GetMergedMovieDetails()
        {
            if (_cache == null)
            {
                _logger.LogWarning("Redis cache is unavailable.");
                return StatusCode(500, "Redis cache is unavailable.");
            }

            var cachedData = await _cache.StringGetAsync("merged_movie_details");

            if (cachedData.IsNullOrEmpty)
            {
                _logger.LogWarning("No merged movie details found in cache.");
                return NotFound("No merged movie details found.");
            }

            try
            {
                var mergedMovies = JsonSerializer.Deserialize<List<MergedMovieDetail>>(cachedData!) ?? new List<MergedMovieDetail>();
                _logger.LogInformation($"Returning {mergedMovies.Count} merged movie details.");
                return Ok(mergedMovies);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize merged movie details from cache.");
                return StatusCode(500, "Error parsing cached movie details.");
            }
        }
    }
}
