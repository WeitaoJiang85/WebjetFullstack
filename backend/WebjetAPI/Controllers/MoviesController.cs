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

    [HttpGet("allmovies")]
    public async Task<IActionResult> GetAllMovies()
    {
        try
        {
            if (_cache == null)
            {
                _logger.LogWarning("Redis cache is unavailable.");
                return StatusCode(500, "Redis is unavailable.");
            }

            var cachedData = await _cache.StringGetAsync("merged_movies");
            if (cachedData.IsNullOrEmpty)
            {
                _logger.LogWarning("No merged movie data found in cache.");
                return NotFound("No merged movie data available.");
            }

            var mergedMovies = !cachedData.IsNullOrEmpty ? JsonSerializer.Deserialize<List<MergedMovie>>(cachedData.ToString()) : new List<MergedMovie>();
            return Ok(mergedMovies ?? new List<MergedMovie>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all movies from cache");
            return StatusCode(500, "Internal server error");
        }
    }
}

}
