using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebjetAPI.Models;

namespace WebjetAPI.Services;

/// <summary>
/// Background service that periodically fetches basic movie data from providers and stores it in Redis.
/// </summary>
public class MovieCacheService : BackgroundService
{
    private readonly ILogger<MovieCacheService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IDatabase? _cache;
    private readonly MovieDetailCacheService _movieDetailCacheService;
    private readonly string _cinemaworldBaseUrl;
    private readonly string _filmworldBaseUrl;
    private readonly string _apiToken;

    public MovieCacheService(
        ILogger<MovieCacheService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IConnectionMultiplexer? redis,
        MovieDetailCacheService movieDetailCacheService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = redis?.GetDatabase();
        _movieDetailCacheService = movieDetailCacheService;

        _cinemaworldBaseUrl = _configuration["WebjetAPI:BaseUrls:cinemaworld"] ?? throw new ArgumentNullException("cinemaworld API URL is missing");
        _filmworldBaseUrl = _configuration["WebjetAPI:BaseUrls:filmworld"] ?? throw new ArgumentNullException("filmworld API URL is missing");
        _apiToken = _configuration["WebjetAPI:ApiToken"] ?? throw new ArgumentNullException("API Token is missing");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting movie data cache refresh");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var movies = await FetchMoviesWithRetries(client);

                if (_cache != null)
                {
                    if (movies.TryGetValue("cinemaworld", out var cinemaMovies))
                    {
                        var cinemaCacheData = JsonSerializer.Serialize(cinemaMovies);
                        await _cache.StringSetAsync("cinemaworld_movies", cinemaCacheData, TimeSpan.FromDays(1));
                        _logger.LogInformation("Updated cinemaworld movie data in cache");
                    }

                    if (movies.TryGetValue("filmworld", out var filmMovies))
                    {
                        var filmCacheData = JsonSerializer.Serialize(filmMovies);
                        await _cache.StringSetAsync("filmworld_movies", filmCacheData, TimeSpan.FromDays(1));
                        _logger.LogInformation("Updated filmworld movie data in cache");
                    }

                    // Generate merged movie list and store in Redis
                    var mergedMovies = GenerateMergedMovies(movies);
                    var mergedMoviesCacheData = JsonSerializer.Serialize(mergedMovies);
                    await _cache.StringSetAsync("merged_movies", mergedMoviesCacheData, TimeSpan.FromDays(1));
                    _logger.LogInformation("Updated merged movie data in cache");
                }
                else
                {
                    _logger.LogWarning("Redis cache is unavailable");
                }

                // Trigger detailed movie data update after refreshing movie cache
                await _movieDetailCacheService.RunDetailUpdateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie cache");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    /// <summary>
    /// Fetches movie lists from providers with up to 3 retries.
    /// </summary>
    private async Task<Dictionary<string, List<Movie>>> FetchMoviesWithRetries(HttpClient client)
    {
        var allMovies = new Dictionary<string, List<Movie>>
        {
            { "cinemaworld", new List<Movie>() },
            { "filmworld", new List<Movie>() }
        };

        var providers = new Dictionary<string, string>
        {
            { "cinemaworld", _cinemaworldBaseUrl },
            { "filmworld", _filmworldBaseUrl }
        };

        foreach (var provider in providers.Keys)
        {
            bool success = false;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var moviesUrl = $"{providers[provider]}/movies";
                    _logger.LogInformation($"Fetching movies from {provider} (Attempt {attempt}/3)");
                    var request = new HttpRequestMessage(HttpMethod.Get, moviesUrl);
                    request.Headers.Add("x-access-token", _apiToken);
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var movies = JsonSerializer.Deserialize<MovieResponse>(await response.Content.ReadAsStringAsync())?.Movies ?? new List<Movie>();
                    allMovies[provider] = movies;
                    success = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to fetch movies from {provider} (Attempt {attempt}/3)");
                    if (attempt < 3)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                }
            }

            // If all attempts failed, preserve the existing Redis cache
            if (!success && _cache != null)
            {
                var providerKey = provider;
                var cachedData = await _cache.StringGetAsync($"{providerKey}_movies");

                if (!cachedData.IsNullOrEmpty)
                {
                    allMovies[provider] = JsonSerializer.Deserialize<List<Movie>>(cachedData.ToString()) ?? new List<Movie>();
                    _logger.LogWarning($"Using existing cache for {provider} as all retries failed.");
                }
                else
                {
                    _logger.LogError($"No cache available for {provider} and all retries failed. The movie list will be empty.");
                }
            }
        }

        return allMovies;
    }

    /// <summary>
    /// Merges movies from cinemaworld and filmworld, ensuring unique movies by title.
    /// </summary>
    private List<MergedMovie> GenerateMergedMovies(Dictionary<string, List<Movie>> movies)
    {
        var mergedMovies = new Dictionary<string, MergedMovie>();

        // Iterate through cinemaworld movies and add them to the dictionary first
        foreach (var movie in movies["cinemaworld"])
        {
            // Ensure the movie has a valid title before processing
            if (!string.IsNullOrWhiteSpace(movie.Title))
            {
                mergedMovies[movie.Title] = new MergedMovie
                {
                    Title = movie.Title,
                    Year = movie.Year ?? "Unknown",
                    Type = movie.Type ?? "Unknown",
                    Poster = movie.Poster,
                    IDs = !string.IsNullOrEmpty(movie.ID) ? new List<string> { movie.ID } : new List<string>(),
                    RawID = movie.ID ?? string.Empty // Use the first available ID initially
                };
            }
        }

        // Iterate through filmworld movies and merge them with existing ones
        foreach (var movie in movies["filmworld"])
        {
            // Ensure the movie has a valid title before processing
            if (!string.IsNullOrWhiteSpace(movie.Title))
            {
                // If the movie exists in the dictionary (from cinemaworld), update it
                if (mergedMovies.TryGetValue(movie.Title, out var existingMovie))
                {
                    if (!string.IsNullOrEmpty(movie.ID))
                    {
                        existingMovie.IDs.Add(movie.ID); // Add the filmworld ID to the list
                    }
                    existingMovie.RawID = string.Join("-", existingMovie.IDs); // Combine IDs for RawID
                }
                else
                {
                    // If the movie does not exist, add it as a new entry
                    mergedMovies[movie.Title] = new MergedMovie
                    {
                        Title = movie.Title,
                        Year = movie.Year ?? "Unknown",
                        Type = movie.Type ?? "Unknown",
                        Poster = movie.Poster,
                        IDs = !string.IsNullOrEmpty(movie.ID) ? new List<string> { movie.ID } : new List<string>(),
                        RawID = movie.ID ?? string.Empty // Use the first available ID initially
                    };
                }
            }
        }

        return mergedMovies.Values.ToList();
    }
}
