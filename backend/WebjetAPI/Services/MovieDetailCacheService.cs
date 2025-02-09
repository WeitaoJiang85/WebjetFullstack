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

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
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
                    ? JsonSerializer.Deserialize<List<Movie>>(cinemaData) ?? new List<Movie>()
                    : new List<Movie>();

                var filmData = await _cache.StringGetAsync("filmworld_movies");
                cachedMovies["filmworld"] = !string.IsNullOrEmpty(filmData)
                    ? JsonSerializer.Deserialize<List<Movie>>(filmData) ?? new List<Movie>()
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

        try
        {
            string? baseUrl = _configuration[$"WebjetAPI:BaseUrls:{provider.Capitalize()}"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning($"Base URL for {provider} is missing. Skipping movie details fetch.");
                return false;
            }

            var movieUrl = $"{baseUrl}/movie/{movieId}";
            _logger.LogInformation($"Fetching movie details from {movieUrl}");

            var request = new HttpRequestMessage(HttpMethod.Get, movieUrl);
            request.Headers.Add("x-access-token", _apiToken);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var movieDetails = JsonSerializer.Deserialize<MovieDetail>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            });

            if (movieDetails != null)
            {
            
                movieDetails.Provider = provider.Capitalize();

                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(movieDetails), TimeSpan.FromMinutes(10));
                _logger.LogInformation($"Cached details for {movieDetails.Title} ({movieId}) from {movieDetails.Provider}");
                return true;
            }
            else
            {
                _logger.LogWarning($"Movie details for {movieId} from {provider} are null. Skipping cache.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to fetch details for {movieId} from {provider}, keeping previous cache.");
            return false;
        }
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
