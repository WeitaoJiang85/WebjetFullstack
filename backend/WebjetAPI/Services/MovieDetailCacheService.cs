using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebjetAPI.Models;

namespace WebjetAPI.Services;

/// <summary>
/// Background service that periodically fetches detailed movie data from providers and stores it in Redis.
/// </summary>
public class MovieDetailCacheService : BackgroundService
{
    private readonly ILogger<MovieDetailCacheService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IDatabase? _cache;
    private readonly string _apiToken;

    public MovieDetailCacheService(ILogger<MovieDetailCacheService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration, IConnectionMultiplexer? redis)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = redis?.GetDatabase();
        _apiToken = _configuration["WebjetAPI:ApiToken"] ?? throw new ArgumentNullException("API Token is missing");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting scheduled movie detail data cache refresh");

            await RunDetailUpdateAsync();

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    /// <summary>
    /// Public method that triggers an update of movie details, used by MovieCacheService.
    /// </summary>
    public async Task RunDetailUpdateAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var cachedMovies = await FetchMoviesFromCache();

            _logger.LogInformation($"Found {cachedMovies["cinemaworld"].Count} movies in cinemaworld cache");
            _logger.LogInformation($"Found {cachedMovies["filmworld"].Count} movies in filmworld cache");

            var failedRequests = new List<(string Provider, string MovieId)>();

            foreach (var provider in cachedMovies.Keys)
            {
                foreach (var movie in cachedMovies[provider])
                {
                    if (!string.IsNullOrEmpty(movie.ID))
                    {
                        bool success = await FetchAndCacheMovieDetails(client, provider, movie.ID);
                        if (!success)
                        {
                            failedRequests.Add((provider, movie.ID));
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Skipping movie with null ID from {provider}");
                    }
                }
            }

            // Retry fetching failed movie details once
            if (failedRequests.Count > 0)
            {
                _logger.LogWarning($"Retrying {failedRequests.Count} failed movie detail fetches");

                foreach (var (provider, movieId) in failedRequests)
                {
                    await FetchAndCacheMovieDetails(client, provider, movieId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating movie detail cache");
        }
    }

    private async Task<Dictionary<string, List<Movie>>> FetchMoviesFromCache()
    {
        var cachedMovies = new Dictionary<string, List<Movie>>
        {
            { "cinemaworld", new List<Movie>() },
            { "filmworld", new List<Movie>() }
        };

        try
        {
            if (_cache != null)
            {
                var cinemaData = await _cache.StringGetAsync("cinemaworld_movies");
                cachedMovies["cinemaworld"] = !string.IsNullOrEmpty(cinemaData)
                    ? JsonSerializer.Deserialize<List<Movie>>(cinemaData!) ?? new List<Movie>()
                    : new List<Movie>();

                var filmData = await _cache.StringGetAsync("filmworld_movies");
                cachedMovies["filmworld"] = !string.IsNullOrEmpty(filmData)
                    ? JsonSerializer.Deserialize<List<Movie>>(filmData!) ?? new List<Movie>()
                    : new List<Movie>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch cached movies");
        }

        return cachedMovies;
    }

 private async Task<bool> FetchAndCacheMovieDetails(HttpClient client, string provider, string movieId)
{
    string cacheKey = $"{provider}_movie_details:{movieId}";
    string? baseUrl = _configuration[$"WebjetAPI:BaseUrls:{provider.Capitalize()}"];
    
    if (string.IsNullOrEmpty(baseUrl))
    {
        _logger.LogWarning($"Base URL for {provider} is missing. Skipping movie details fetch.");
        return false;
    }

    var movieUrl = $"{baseUrl}/movie/{movieId}";
    _logger.LogInformation($"Fetching movie details from {movieUrl}");

    int maxRetries = 2;  // Retry up to 2 times
    int delayMilliseconds = 2000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, movieUrl);
            request.Headers.Add("x-access-token", _apiToken);

            HttpResponseMessage response = await client.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                if (_cache != null)
                {
                    await _cache.StringSetAsync(cacheKey, json);
                }
                else
                {
                    _logger.LogWarning("Cache is not initialized.");
                }
                _logger.LogInformation($"Cached details for {movieId} from {provider}");
                return true;
            }
            else
            {
                _logger.LogWarning($"Failed attempt {attempt}/{maxRetries} for {movieId} from {provider}: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request error for {movieId} from {provider}, attempt {attempt}: {ex.Message}");
        }

        if (attempt < maxRetries)
        {
            await Task.Delay(delayMilliseconds);  // Wait before retrying
        }
    }

    _logger.LogError($"Failed to fetch details for {movieId} from {provider} after {maxRetries} attempts.");
    return false;
}
}

/// <summary>
/// Extension method to capitalize provider names for configuration lookup.
/// </summary>
public static class StringExtensions
{
    public static string Capitalize(this string input)
    {
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
